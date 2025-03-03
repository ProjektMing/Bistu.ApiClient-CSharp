using Bistu.Api.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Bistu.Api.Console;

internal class Program
{
    public static void Main()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddSerilog());
        ILogger<Authenticator> logger = factory.CreateLogger<Authenticator>();

        Authenticator bistuAuthenticator =
            new Authenticator().AsPassword("Ming", "123456");
        bistuAuthenticator.LogoutAsync();
    }
}
