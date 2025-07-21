namespace Bistu.Api.Models;

/// <summary>
/// 表单提交类，根据认证策略构建不同的表单内容
/// </summary>
internal class SubmitForm
{
    /// <summary>
    /// 认证策略，决定表单包含哪些参数
    /// </summary>
    public AuthenticationStrategy Strategy { get; set; } = AuthenticationStrategy.None;
    
    /// <summary>
    /// 用户名（用于密码认证）
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// 密码（用于密码认证）
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// UUID（用于二维码认证）
    /// </summary>
    public string? Uuid { get; set; }
    
    /// <summary>
    /// 执行令牌（所有认证方式都需要）
    /// </summary>
    public required string Execution { get; set; }

    /// <summary>
    /// 根据认证策略构建表单内容
    /// </summary>
    /// <returns>FormUrlEncodedContent 实例</returns>
    /// <exception cref="NotSupportedException">当认证策略不受支持时抛出</exception>
    public FormUrlEncodedContent Build()
    {
        return Strategy switch
        {
            AuthenticationStrategy.None => new([]),
            AuthenticationStrategy.QrCode => BuildQrCodeForm(),
            AuthenticationStrategy.UsernameAndPassword => BuildUsernameForm(),
            _ => throw new NotSupportedException($"不支持的认证策略: {Strategy}")
        };
    }

    /// <summary>
    /// 构建用户名密码认证表单
    /// </summary>
    private FormUrlEncodedContent BuildUsernameForm()
    {
        if (string.IsNullOrEmpty(Username))
            throw new ArgumentNullException(nameof(Username), "用户名不能为空");
        if (string.IsNullOrEmpty(Password))
            throw new ArgumentNullException(nameof(Password), "密码不能为空");
            
        return new FormUrlEncodedContent([
            new("username", Username),
            new("password", Password),
            new("captcha", ""),
            new("_eventId", "submit"),
            new("cllt", "userNameLogin"),
            new("dllt", "generalLogin"),
            new("lt", ""),
            new("execution", Execution)
        ]);
    }

    /// <summary>
    /// 构建二维码认证表单
    /// </summary>
    private FormUrlEncodedContent BuildQrCodeForm()
    {
        if (string.IsNullOrEmpty(Uuid))
            throw new ArgumentException("UUID不能为空", nameof(Uuid));
            
        return new FormUrlEncodedContent([
            new("lt", ""),
            new("uuid", Uuid),
            new("cllt", "qrLogin"),
            new("dllt", "generalLogin"),
            new("execution", Execution),
            new("_eventId", "submit")
        ]);
    }
}
