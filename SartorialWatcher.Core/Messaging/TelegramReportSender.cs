using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Core;

namespace SartorialWatcher.Core.Messaging;

public class TelegramReportSender(HttpClient http, IConfiguration configuration, ILogger<TelegramReportSender> logger)
    : IReportSender
{
    private string SendMessageEndpoint(string botToken) => $"https://api.telegram.org/bot{botToken}/sendMessage";

    public async Task SendReport(string message)
    {
        logger.LogInformation("Requested to send report via Telegram");

        var botToken = configuration["Telegram:Bot:Token"] ??
                       throw new InvalidOperationException("Missing Telegram bot token in app configuration");

        var chatId = configuration["Telegram:Chat:Id"] ??
                     throw new InvalidOperationException("Missing Telegram chat id in app configuration");

        var endpoint = SendMessageEndpoint(botToken);

        var body = new
        {
            chat_id = chatId,
            text = message,
            parse_mode = "HTML",
            link_preview_options = new
            {
                is_disabled = true
            }
        };

        await http.PostAsJsonAsync(endpoint, body);
    }
}