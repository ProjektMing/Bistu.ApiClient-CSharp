using Bistu.Api.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Bistu.Api.Console;

internal static class Program
{
    public static async Task Main()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddSerilog());
        ILogger<Authenticator> logger = factory.CreateLogger<Authenticator>();

        Authenticator bistuAuthenticator =
            Authenticator.Create().AsPassword("Ming", "123456");
        await bistuAuthenticator.LogoutAsync();
    }
}
