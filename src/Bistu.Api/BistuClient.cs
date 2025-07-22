using System.Net;
using Microsoft.Extensions.Logging;

namespace Bistu.Api;

/// <summary>
/// Client for interacting with BISTU's academic system
/// </summary>
public class BistuClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Authenticator _authenticator;
    private readonly bool _ownsHttpClient;
    private bool _disposed;
    private ILogger<BistuClient>? _logger;

    /// <summary>
    /// Cookie container for maintaining session state
    /// </summary>
    public CookieContainer CookieContainer => _authenticator.CookieContainer;

    /// <summary>
    /// CAS server address
    /// </summary>
    public Uri CasAddress { get; set; } = new("https://wxjw.bistu.edu.cn/authserver");

    /// <summary>
    /// Portal server address
    /// </summary>
    public Uri PortalAddress { get; set; } = new("https://jwxt.bistu.edu.cn");

    /// <summary>
    /// Initializes a new instance of BistuClient with a provided HttpClient
    /// </summary>
    /// <param name="httpClient">The HttpClient to use for requests</param>
    public BistuClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _ownsHttpClient = false;
        _authenticator = new Authenticator(CookieContainer)
        {
            // 同步门户地址
            PortalAddress = PortalAddress
        };
        _logger?.LogDebug("初始化");
    }

    /// <summary>
    /// Initializes a new instance of BistuClient with default configuration
    /// </summary>
    public BistuClient()
        : this(CreateDefaultHttpClient())
    {
        _ownsHttpClient = true;
    }

    /// <summary>
    /// 执行登录操作
    /// </summary>
    /// <returns>登录是否成功</returns>
    /// <exception cref="InvalidOperationException">当未配置认证策略或登录失败时抛出</exception>
    /// <exception cref="ObjectDisposedException">当客户端已被释放时抛出</exception>
    public async Task<bool> LoginAsync(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            // 同步门户地址（以防用户在配置后更改了地址）
            _authenticator.PortalAddress = PortalAddress;

            return await _authenticator.LoginAsync(token);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("BISTU authentication failed.", ex);
        }
    }

    public Task<bool> LoginAsync() => LoginAsync(CancellationToken.None);

    public Task<HttpResponseMessage> GetAsync(string url, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _httpClient.GetAsync(url, token);
    }

    #region 认证策略配置
    /// <summary>
    /// 配置使用二维码认证
    /// </summary>
    /// <param name="qrCodeHandler">处理二维码 URL 的回调方法，参数为二维码图片的 URL</param>
    /// <returns>当前 BistuClient 实例，支持方法链式调用</returns>
    /// <exception cref="ArgumentNullException">当 qrCodeHandler 为 null 时抛出</exception>
    public BistuClient UseQrCode(Action<string>? qrCodeHandler = null)
    {
        _authenticator.UseQrCode(qrCodeHandler);
        return this;
    }

    /// <summary>
    /// 配置使用用户名密码认证
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>当前 BistuClient 实例，支持方法链式调用</returns>
    /// <exception cref="ArgumentException">当用户名或密码为空时抛出</exception>
    public BistuClient UsePassword(string username, string password)
    {
        _authenticator.UsePassword(username, password);
        return this;
    }
    #endregion

    /// <summary>
    /// Creates a default HttpClient with appropriate configuration for BISTU services
    /// </summary>
    private static HttpClient CreateDefaultHttpClient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://wxjw.bistu.edu.cn/authserver"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public BistuClient SetLogger(ILogger<BistuClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticator.SetLogger((ILogger<Authenticator>)logger);
        return this;
    }

    #region IDisposable 实现
    /// <summary>
    /// Releases the unmanaged resources used by the BistuClient and optionally releases the managed resources
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _authenticator?.Dispose();

                if (_ownsHttpClient)
                {
                    _httpClient?.Dispose();
                }
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Releases all resources used by the BistuClient
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
