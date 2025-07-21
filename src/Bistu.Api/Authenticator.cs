using System.Net;
using System.Text.RegularExpressions;
using Bistu.Api.Models;
using Bistu.Api.Models.QrCode;

namespace Bistu.Api;

/// <summary>
/// 处理 BISTU 系统认证的类
/// </summary>
public partial class Authenticator : IDisposable
{
    private readonly HttpClient _client;
    private readonly CookieContainer _cookieContainer;
    private Action<string>? _qrCodeAction;
    private readonly Func<string, string> _extractExecutionToken;
    private readonly SubmitForm _form;
    private bool _disposed;

    /// <summary>
    /// 基础 URL 地址
    /// </summary>
    public UriBuilder CasAddress => new("https://wxjw.bistu.edu.cn/authserver/");

    /// <summary>
    /// 教务系统门户 URL
    /// </summary>
    public UriBuilder PortalAddress { get; set; } = new("https://jwxt.bistu.edu.cn/jwapp");

    /// <summary>
    /// Cookie 容器，用于维护会话状态
    /// </summary>
    public CookieContainer CookieContainer => _cookieContainer;

    public Authenticator(Func<string, string>? func)
    {
        _cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true
        };
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://wxjw.bistu.edu.cn/authserver/")
        };

        _extractExecutionToken =
            func ?? (input => ExecutionValueRegex().Match(input).Groups["execution"].Value);

        // 获取执行令牌
        var execution = GetExecutionToken();
        _form = new SubmitForm { Execution = execution };
    }

    [GeneratedRegex(@"(?<=execution[^>]+)value=""(?<execution>.+)""", RegexOptions.RightToLeft)]
    private static partial Regex ExecutionValueRegex();

    /// <summary>
    /// 获取执行令牌
    /// </summary>
    private string GetExecutionToken()
    {
        try
        {
            var content = _client.GetStringAsync("./login").GetAwaiter().GetResult();
            return _extractExecutionToken(content)
                ?? throw new InvalidOperationException("Execution token not found.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to get execution token.", ex);
        }
    }

    /// <summary>
    /// 执行登录操作
    /// </summary>
    /// <returns>登录是否成功</returns>
    public async Task<bool> LoginAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            return _form.Strategy switch
            {
                AuthenticationStrategy.QrCode => await LoginWithQrCodeAsync(),
                AuthenticationStrategy.UsernameAndPassword => await LoginWithPasswordAsync(),
                AuthenticationStrategy.None
                    => throw new InvalidOperationException(
                        "Authentication strategy must be set before login."
                    ),
                _
                    => throw new NotSupportedException(
                        $"Unsupported authentication strategy: {_form.Strategy}"
                    )
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Login failed.", ex);
        }
    }

    /// <summary>
    /// 使用用户名密码登录
    /// </summary>
    private async Task<bool> LoginWithPasswordAsync()
    {
        if (string.IsNullOrEmpty(_form.Username) || string.IsNullOrEmpty(_form.Password))
        {
            throw new InvalidOperationException(
                "Username and password must be set for password authentication."
            );
        }

        var content = _form.Build();
        var response = await _client.PostAsync("./login", content);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        // 完成门户登录
        await CompletePortalLoginAsync();
        return true;
    }

    /// <summary>
    /// 使用二维码登录
    /// </summary>
    private async Task<bool> LoginWithQrCodeAsync()
    {
        // 获取二维码 UUID
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var uuid = await _client.GetStringAsync($"./qrCode/getToken?ts={timestamp}");

        if (string.IsNullOrWhiteSpace(uuid))
        {
            throw new InvalidOperationException("Failed to get QR code UUID.");
        }

        // 设置 UUID 到表单
        _form.Uuid = uuid;

        // 通知外部处理器显示二维码
        var qrCodeUrl = $"./qrCode/getCode?uuid={uuid}";
        _qrCodeAction?.Invoke(qrCodeUrl);

        // 初始化二维码登录会话
        await _client.GetAsync($"./qrCode/qrCodeLogin.do?uuid={uuid}");

        // 轮询二维码状态
        LoginStatus status;
        do
        {
            await Task.Delay(800);
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var statusResponse = await _client.GetStringAsync(
                $"./qrCode/getStatus.htl?ts={timestamp}&uuid={uuid}"
            );

            if (!int.TryParse(statusResponse, out var statusCode))
            {
                throw new InvalidOperationException(
                    $"Failed to parse status code from response: {statusResponse}"
                );
            }

            status = (LoginStatus)statusCode;

            switch (status)
            {
                case LoginStatus.Expired:
                    throw new InvalidOperationException("QR code login failed: QR code expired.");
                case LoginStatus.Scanned:
                    // 继续等待
                    break;
                case LoginStatus.Success:
                    // 登录成功，退出循环
                    break;
            }
        } while (status != LoginStatus.Success);

        // 提交最终登录表单
        await PostQrCodeLoginAsync(uuid);

        // 完成门户登录
        await CompletePortalLoginAsync();

        return true;
    }

    /// <summary>
    /// 提交二维码登录的最终表单
    /// </summary>
    private async Task PostQrCodeLoginAsync(string uuid)
    {
        _form.Uuid = uuid;
        var content = _form.Build();
        var response = await _client.PostAsync("./login", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"QR code login post failed with status: {response.StatusCode}"
            );
        }
    }

    /// <summary>
    /// 完成门户登录过程
    /// </summary>
    private async Task CompletePortalLoginAsync()
    {
        // 查找票据 Cookie
        var ticketCookie =
            FindCookie("MOD_AUTH_CAS")
            ?? throw new InvalidOperationException("Failed to get ticket cookie (MOD_AUTH_CAS).");

        // 提取票据（跳过前 9 个字符）
        string ticket =
            ticketCookie.Value.Length > 9
                ? ticketCookie.Value[9..]
                : throw new InvalidOperationException("Invalid ticket cookie format.");

        // 使用票据访问门户
        var portalResponse = await _client.GetAsync($"{PortalAddress}/index.do?ticket={ticket}");

        if (!portalResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Portal access failed with status: {portalResponse.StatusCode}"
            );
        }

        // 验证是否获得了 _WEU Cookie
        var weuCookie =
            FindCookie("_WEU")
            ?? throw new InvalidOperationException("Failed to get _WEU cookie from portal.");
    }

    /// <summary>
    /// 查找指定名称的 Cookie
    /// </summary>
    private Cookie? FindCookie(string name)
    {
        foreach (Cookie cookie in _cookieContainer.GetCookies(CasAddress.Uri))
        {
            if (cookie.Name == name)
            {
                return cookie;
            }
        }

        // 也检查门户 URL 的 Cookies
        foreach (Cookie cookie in _cookieContainer.GetCookies(PortalAddress.Uri))
        {
            if (cookie.Name == name)
            {
                return cookie;
            }
        }

        return null;
    }

    #region 配置方法
    /// <summary>
    /// 配置使用二维码认证
    /// </summary>
    /// <param name="action">处理二维码 URL 的回调方法</param>
    public Authenticator UseQrCode(Action<string> action)
    {
        _qrCodeAction = action ?? throw new ArgumentNullException(nameof(action));
        _form.Strategy = AuthenticationStrategy.QrCode;
        return this;
    }

    /// <summary>
    /// 配置使用用户名密码认证
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    public Authenticator UsePassword(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        _form.Username = username;
        _form.Password = password;
        _form.Strategy = AuthenticationStrategy.UsernameAndPassword;
        return this;
    }
    #endregion

    #region IDisposable 实现
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _client?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
