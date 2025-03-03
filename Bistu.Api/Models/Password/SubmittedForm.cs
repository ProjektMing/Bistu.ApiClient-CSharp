namespace Bistu.Api.Models.Password;

public class SubmittedForm(string executionValue)
{
    public FormUrlEncodedContent Content { get; } = new([
        new KeyValuePair<string, string>("username", ""),
        new KeyValuePair<string, string>("password", ""),
        new KeyValuePair<string, string>("captcha", ""),
        new KeyValuePair<string, string>("_eventId", "submit"),
        new KeyValuePair<string, string>("cllt", "userNameLogin"),
        new KeyValuePair<string, string>("dllt", "generalLogin"),
        new KeyValuePair<string, string>("lt", ""),
        new KeyValuePair<string, string>("execution", executionValue)
    ]);
}
