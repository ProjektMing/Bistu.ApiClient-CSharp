namespace Bistu.Api.Models.QrCode;

public class SubmittedForm(string uuid, string executionValue)
{
    public FormUrlEncodedContent Content = new([
        new KeyValuePair<string, string>("lt", ""),
        new KeyValuePair<string, string>("uuid", uuid),
        new KeyValuePair<string, string>("cllt", "qrLogin"),
        new KeyValuePair<string, string>("dllt", "generalLogin"),
        new KeyValuePair<string, string>("execution", executionValue),
        new KeyValuePair<string, string>("_eventId", "submit")
    ]);
}
