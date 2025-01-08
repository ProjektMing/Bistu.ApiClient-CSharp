using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BistuAuthService
{
    public partial class BistuAuthenticator(
        ILogger<BistuAuthenticator> logger,
        HttpClient httpClient,
        CookieContainer cookieContainer
    )
    {
        [GeneratedRegex(@"<input[^>]*name=""execution""[^>]*value=""([^""]*)""")]
        private static partial Regex ExecutionExtractorRegex();

        private const string BaseUrl = "https://wxjw.bistu.edu.cn/authserver";
        private const string PortalUrl = "https://jwxt.bistu.edu.cn/jwapp/sys/emaphome/portal";
        private readonly HttpClient client =
            httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        private readonly ILogger<BistuAuthenticator> _logger = logger;

        public CookieContainer CookieContainer { get; set; } =
            cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));

        private void InitializeHttp()
        {
            client.DefaultRequestHeaders.Add("User-Agent", "qrLogin");
        }

        private static string ExtractExecutionValue(string htmlInput)
        {
            var regex = ExecutionExtractorRegex();
            var match = regex.Match(htmlInput);
            return match.Groups[1].Value;
        }

        public async Task<bool> AuthorizeAsync()
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

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo($"{BaseUrl}/qrCode/getCode?uuid={uuid}")
                    {
                        UseShellExecute = true
                    }
                );
                await client.GetAsync($"{BaseUrl}/qrCode/qrCodeLogin.do?uuid={uuid}");

                int statusCode;
                _logger.LogInformation("Checking QR code status...");
                do
                {
                    await Task.Delay(800);
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var statusResponse = await client.GetStringAsync(
                        $"{BaseUrl}/qrCode/getStatus.htl?ts={timestamp}&uuid={uuid}"
                    );
                    if (!int.TryParse(statusResponse, out statusCode))
                    {
                        _logger.LogError(
                            "Failed to parse status code. As the Response is {statusResponse}",
                            statusResponse
                        );
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
                await PostLoginAsync(uuid, executionValue);
                Cookie ticketCookie =
                    CookieContainer.GetAllCookies().FirstOrDefault(c => c.Name == "MOD_AUTH_CAS")
                    ?? throw new Exception("Failed to get ticket cookie.");
                string ticket = ticketCookie.Value.AsSpan(9).ToString();
                await client.GetAsync($"{PortalUrl}/index.do?ticket={ticket}");
                _ = cookieContainer.GetAllCookies().First(c => c.Name == "_WEU");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during authentication.");
                return false;
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

        private async Task PostLoginAsync(string uuid, string executionValue)
        {
            using (
                FormUrlEncodedContent loginContent =
                    new(
                        [
                            new KeyValuePair<string, string>("lt", ""),
                            new KeyValuePair<string, string>("uuid", uuid),
                            new KeyValuePair<string, string>("cllt", "qrLogin"),
                            new KeyValuePair<string, string>("dllt", "generalLogin"),
                            new KeyValuePair<string, string>("execution", executionValue),
                            new KeyValuePair<string, string>("_eventId", "submit"),
                            new KeyValuePair<string, string>("rmShown", "1"),
                        ]
                    )
            )
            {
                await client.PostAsync("https://wxjw.bistu.edu.cn/authserver/login", loginContent);
            }

            _logger.LogInformation("Login Form posted");
        }

        public async Task<string> FetchScheduleAsync()
        {
            var httpResponse = await client.PostAsync(
                "https://jwxt.bistu.edu.cn/jwapp/sys/wdkb/modules/xskcb.do",
                new FormUrlEncodedContent([new KeyValuePair<string, string>("*json", "1")])
            );
            return await httpResponse.Content.ReadAsStringAsync();
        }
    }
}
