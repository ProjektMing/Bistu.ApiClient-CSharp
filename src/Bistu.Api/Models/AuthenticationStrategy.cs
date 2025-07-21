namespace Bistu.Api.Models;

internal enum AuthenticationStrategy
{
    /// <summary>
    /// 不应做任何操作，且不应抛出异常。
    /// </summary>
    None,

    QrCode,

    UsernameAndPassword,
}

public static class AuthenticationStrategyExtensions
{
    static bool IsValid(this AuthenticationStrategy strategy)
    {
        return strategy switch
        {
            AuthenticationStrategy.None => true,
            AuthenticationStrategy.QrCode => true,
            AuthenticationStrategy.UsernameAndPassword => true,
            _ => false
        };
    }
}
