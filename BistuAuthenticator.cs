using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace CASLogin
{
    public partial class BistuAuthenticator
    {
        [GeneratedRegex(@"<input[^>]*name=""execution""[^>]*value=""([^""]*)""")]
        private static partial Regex ExecutionExtractorRegex();

        private const string BaseUrl = "https://wxjw.bistu.edu.cn/authserver";
        private const string PortalUrl = "https://jwxt.bistu.edu.cn/jwapp/sys/emaphome/portal";
        private readonly HttpClient client;
        private readonly ILogger<BistuAuthenticator> _logger;

        public CookieContainer CookieContainer { get; set; }

        public BistuAuthenticator(ILogger<BistuAuthenticator> logger, [Optional] HttpClient httpClient)
        {
            _logger = logger;
            CookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = CookieContainer
            };

            client = httpClient ?? new HttpClient(handler);
        }

        private void InitializeHttp()
        {
            client.DefaultRequestHeaders.Add("User-Agent", "qrLogin");
            client.DefaultRequestHeaders.Add("Origin", "http://wxjw.bistu.edu.cn");
            client.DefaultRequestHeaders.Add("Referer", $"http://wxjw.bistu.edu.cn/login?service={PortalUrl}/index.do");
            _logger.LogInformation("Adding initial cookies to the CookieContainer.");
            CookieContainer.Add(new Cookie("route", "b19e6014fb75f45c1ec0aab119bd4e8e", "/authserver", "wxjw.bistu.edu.cn"));
            CookieContainer.Add(new Cookie("platformMultilingual", "zh_CN", "/", "edu.cn"));
            CookieContainer.Add(new Cookie("route", "ad55296587da6765d9ff0a1e7203b2c2", "/personalInfo", "wxjw.bistu.edu.cn"));
            CookieContainer.Add(new Cookie("route", "c83e11eebb8da619ef6a94bc26688b29", "/", "jwxt.bistu.edu.cn"));
        }

        private static string ExtractExecutionValue(string htmlInput)
        {
            var regex = ExecutionExtractorRegex();
            var match = regex.Match(htmlInput);
            return match.Groups[1].Value;
        }

        public async Task<CookieContainer> AuthenticateAsync()
        {
            try
            {
                _logger.LogInformation("Starting authentication process...");
                InitializeHttp();

                var htmlInput = await client.GetStringAsync($"{BaseUrl}/login");
                string executionValue = ExtractExecutionValue(htmlInput);
                _logger.LogInformation("Execution value: {executionValue}", executionValue);
                if (string.IsNullOrEmpty(executionValue))
                {
                    _logger.LogError("Failed to extract execution value from login page.");
                    throw new Exception("Failed to extract execution value from login page.");
                }

                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var uuid = await client.GetStringAsync($"{BaseUrl}/qrCode/getToken?ts={timestamp}");

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"{BaseUrl}/qrCode/getCode?uuid={uuid}") { UseShellExecute = true });
                await client.GetAsync($"{BaseUrl}/qrCode/qrCodeLogin.do?uuid={uuid}");

                int statusCode;
                _logger.LogInformation("Checking QR code status...");
                do
                {
                    await Task.Delay(800);
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var statusResponse = await client.GetStringAsync($"{BaseUrl}/qrCode/getStatus.htl?ts={timestamp}&uuid={uuid}");
                    if (!int.TryParse(statusResponse, out statusCode))
                    {
                        _logger.LogError("Failed to parse status code. As the Response is {statusResponse}", statusResponse);
                    }

                    switch (statusCode)
                    {
                        case 3:
                            _logger.LogWarning("QR code expired.");
                            break;
                        case 2:
                            _logger.LogInformation("QR code scanned.");
                            break;
                    }
                } while (statusCode != 1);
                _logger.LogInformation("QR code login successful.");
                await PostLoginAsync(uuid,executionValue);
                var ticketCookie = CookieContainer.GetAllCookies()
                    .FirstOrDefault(c => c.Name == "MOD_AUTH_CAS")
                    ?? throw new Exception("Failed to get ticket cookie.");
                var ticket = ticketCookie.Value.AsSpan(9).ToString();
                await client.GetAsync($"https://jwxt.bistu.edu.cn/jwapp/sys/emaphome/portal/index.do?ticket={ticket}");
                return CookieContainer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during authentication.");
                throw;
            }
        }

        public async Task LogoutAsync()
        {
            _logger.LogInformation("Logging out...");
            if (CookieContainer.GetAllCookies().FirstOrDefault(c => c.Name == "_WEU") is null)
                throw new Exception("Not logged in.");
            var response = await client.GetAsync($"{PortalUrl}/logout.do");
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Logged out successfully.");
        }
        private async Task PostLoginAsync(string uuid,string executionValue)
        {
            using (FormUrlEncodedContent loginContent = new(
                [
                        new KeyValuePair<string, string>("lt", ""),
                        new KeyValuePair<string, string>("uuid", uuid),
                        new KeyValuePair<string, string>("cllt", "qrLogin"),
                        new KeyValuePair<string, string>("dllt", "generalLogin"),
                        new KeyValuePair<string, string>("execution", executionValue),
                        new KeyValuePair<string, string>("_eventId", "submit"),
                        new KeyValuePair<string, string>("rmShown", "1"),
                ]))
            {
                await client.PostAsync("https://wxjw.bistu.edu.cn/authserver/login", loginContent);
            }

            _logger.LogInformation("Login Form posted");
        }
    }
}
