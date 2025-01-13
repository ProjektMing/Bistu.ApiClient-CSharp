using BistuAuthService;
using Microsoft.Extensions.Logging;
using Serilog;

internal class Program
{
    public static async Task Main()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddSerilog());
        var logger = factory.CreateLogger<BistuAuthenticator>();
        HttpClientHandler handler = new() { CookieContainer = new() };
        var httpClient = new HttpClient(handler);

        BistuAuthenticator bistuAuthenticator =
            new(
                logger,
                httpClient,
                handler.CookieContainer,
                s =>
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(s) { UseShellExecute = true }
                    )
            );
        var authStatus = await bistuAuthenticator.AuthorizeAsync();
        if (!authStatus)
        {
            Console.WriteLine("Failed to authenticate.");
            return;
        }
        Console.WriteLine(await bistuAuthenticator.FetchScheduleAsync());
        await Console.In.ReadLineAsync();
        await bistuAuthenticator.LogoutAsync();
    }
}
