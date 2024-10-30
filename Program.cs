using CASLogin;
using Microsoft.Extensions.Logging;
class Program
{
    public static async Task Main()
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = factory.CreateLogger<BistuAuthenticator>();

        BistuAuthenticator bistuAuthenticator = new(logger);
        var authCookies = await bistuAuthenticator.AuthenticateAsync();

        Console.WriteLine($"_WEU: {authCookies.GetAllCookies().FirstOrDefault(c => c.Name == "_WEU")?.Value}");
        await bistuAuthenticator.LogoutAsync();
    }
}
