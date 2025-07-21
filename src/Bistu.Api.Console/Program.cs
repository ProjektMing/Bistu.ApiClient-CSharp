using System;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Bistu.Api.Console;

internal static class Program
{
    public static async Task Main()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(Log.Logger, dispose: true);
        });
        var logger = loggerFactory.CreateLogger(nameof(Program));

        try
        {
            using var client = new BistuClient();

            // 示例1：使用用户名密码认证
            // var success = await client.UsePassword("your_username", "your_password").LoginAsync();

            // 示例2：使用二维码认证
            var success = await client
                .UseQrCode(qrCode =>
                {
                    System.Console.WriteLine($"请扫描二维码登录: {client.CasAddress.Uri + qrCode}");
                    // 这里可以显示二维码或者打开浏览器
                })
                .LoginAsync();

            if (success)
            {
                logger.LogInformation("Login successful.");
                // Add more interactions with the Bistu API here
            }
            else
            {
                logger.LogWarning("Login failed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while interacting with the Bistu API.");
        }
    }
}
