namespace Bistu.Api.Extensions;

public static class PasswordExtension
{
    public static Authenticator AsPassword(this Authenticator authenticator, string username, string password)
    {
        return authenticator;
    }
}
