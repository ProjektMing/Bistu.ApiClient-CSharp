using System.Net;
using System.Text.RegularExpressions;

namespace CASLogin
{
    public class BistuAuthenticator
    {
        private readonly HttpClient client;

        public CookieContainer CookieContainer { get; set; }

        public BistuAuthenticator()
        {
            CookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = CookieContainer
            };

            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Origin", "http://wxjw.bistu.edu.cn");
            client.DefaultRequestHeaders.Add("Referer", "http://wxjw.bistu.edu.cn/authserver/login?service=http://jwxt.bistu.edu.cn/jwapp/sys/emaphome/portal/index.do");
        }

        // Adds initial cookies to the container
        private void AddInitialCookies()
        {
            CookieContainer.Add(new Cookie("route", "b19e6014fb75f45c1ec0aab119bd4e8e", "/authserver", "wxjw.bistu.edu.cn"));
            CookieContainer.Add(new Cookie("platformMultilingual", "zh_CN", "/", "edu.cn"));
            CookieContainer.Add(new Cookie("route", "ad55296587da6765d9ff0a1e7203b2c2", "/personalInfo", "wxjw.bistu.edu.cn"));
            CookieContainer.Add(new Cookie("route", "c83e11eebb8da619ef6a94bc26688b29", "/", "jwxt.bistu.edu.cn"));
        }

        // Extracts the execution token from the HTML response
        private static string ExtractExecutionValue(string htmlInput)
        {
            var regex = new Regex(@"<input[^>]*name=""execution""[^>]*value=""([^""]*)""", RegexOptions.RightToLeft);
            var match = regex.Match(htmlInput);
            return match.Groups[1].Value;
        }

        // Sends login request and handles QR code scanning
        public async Task<CookieContainer?> AuthenticateAsync()
        {
            AddInitialCookies();

            var htmlInput = await client.GetStringAsync("https://wxjw.bistu.edu.cn/authserver/login");
            string executionValue = ExtractExecutionValue(htmlInput);

            if (string.IsNullOrEmpty(executionValue))
            {
                Console.WriteLine("Failed to extract execution value");
                return null;
            }

            // Get UUID for QR code login
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var uuid = await client.GetStringAsync($"https://wxjw.bistu.edu.cn/authserver/qrCode/getToken?ts={timestamp}");

            // Display the QR code for login
            Console.WriteLine($"https://wxjw.bistu.edu.cn/authserver/qrCode/getCode?uuid={uuid}");
            Console.WriteLine($"https://wxjw.bistu.edu.cn/authserver/qrCode/qrCodeLogin.do?uuid={uuid}");

            //await client.GetAsync($"https://wxjw.bistu.edu.cn/authserver/qrCode/getCode?uuid={uuid}");

            // Send login request via QR code
            Console.WriteLine("Sending QR code login request");
            await client.GetAsync($"https://wxjw.bistu.edu.cn/authserver/qrCode/qrCodeLogin.do?uuid={uuid}");

            // Check QR code status
            int statusCode;
            do
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var statusResponse = await client.GetStringAsync($"https://wxjw.bistu.edu.cn/authserver/qrCode/getStatus.htl?ts={timestamp}&uuid={uuid}");
                _ = int.TryParse(statusResponse, out statusCode);
                await Task.Delay(2500);
                if (statusCode == 3)
                {
                    Console.WriteLine("QR code expired");
                    return null;
                }
            } while (1 != statusCode); // Wait until QR code login is successful

            Console.WriteLine("QR code login successful");

            // Final login request
            var loginContent = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("lt", ""),
                new KeyValuePair<string, string>("uuid", uuid),
                new KeyValuePair<string, string>("cllt", "qrLogin"),
                new KeyValuePair<string, string>("dllt", "generalLogin"),
                new KeyValuePair<string, string>("execution", executionValue),
                new KeyValuePair<string, string>("_eventId", "submit"),
                new KeyValuePair<string, string>("rmShown", "1"),
            ]);

            var loginResponse = await client.PostAsync("https://wxjw.bistu.edu.cn/authserver/login", loginContent);
            Console.WriteLine("Login request sent");

            // Redirect to the portal with ticket
            var ticketCookie = CookieContainer.GetAllCookies().FirstOrDefault(c => c.Name == "MOD_AUTH_CAS");
            if (ticketCookie != null)
            {
                var ticket = ticketCookie.Value.AsSpan(9).ToString();
                await client.GetAsync($"https://jwxt.bistu.edu.cn/jwapp/sys/emaphome/portal/index.do?ticket={ticket}");
            }

            return CookieContainer;
        }
    }
}