using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;
using SartorialWatcher.Core.Exceptions;

namespace SartorialWatcher.Core.Messaging;

public class TelegramReportSender(HttpClient http, IConfiguration configuration, ILogger<TelegramReportSender> logger)
    : IReportSender
{
    private string SendMessageEndpoint(string botToken) => $"https://api.telegram.org/bot{botToken}/sendMessage";

    public async Task<bool> SendReport(string message)
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
        
        var response = await http.PostAsJsonAsync(endpoint, body);

        var responseBody = await response.Content.ReadAsStringAsync();

        logger.LogDebug(
            "Telegram response: {StatusCode}, {Body}",
            response.StatusCode,
            responseBody);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        
        logger.LogWarning($"Report was not sent");

        if (response.StatusCode == HttpStatusCode.BadRequest && responseBody.Contains("too long"))
        {
            throw new MessageTooLongException(limit: 4096);
        }

        return false;
    }
    
    private static IEnumerable<string> SplitMessage(
        string message,
        int maxLength = 3700)
    {
        for (var i = 0; i < message.Length; i += maxLength)
        {
            yield return message.Substring(
                i,
                Math.Min(maxLength, message.Length - i));
        }
    }
}