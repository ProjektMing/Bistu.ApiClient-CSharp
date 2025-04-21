namespace Bistu.Api;

public class Authenticator
{
    private Authenticator()
    {
    }

    public static Authenticator Create()
    {
        return new Authenticator();
    }

    private void Logout()
    {
        // Logout logic
    }
}
