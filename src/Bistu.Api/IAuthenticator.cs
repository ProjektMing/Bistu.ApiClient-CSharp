using System.Net;
using Microsoft.Extensions.Logging;

namespace Bistu.Api;

public interface IAuthenticator : IDisposable
{
    Task<bool> LoginAsync(CancellationToken token = default);
    CookieContainer CookieContainer { get; }
    Uri CasAddress { get; set; }
    Uri PortalAddress { get; set; }
    HttpClient Client { get; }

    // 配置方法
    IAuthenticator UseQrCode(Action<string>? handler = null);
    IAuthenticator UsePassword(string username, string password);
    IAuthenticator SetLogger(ILogger logger);
}
