using CASLogin;
class Program
{
    public static async Task Main()
    {
        BistuAuthenticator bistuAuthenticator = new();
        var authCookies = await bistuAuthenticator.AuthenticateAsync();
        if (authCookies == null)
        {
            Console.WriteLine("Failed to authenticate");
            return;
        }
        Console.WriteLine($"_WEU: {authCookies.GetAllCookies().FirstOrDefault(c => c.Name == "_WEU")?.Value}");
    }
}
