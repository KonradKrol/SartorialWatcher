using Microsoft.Extensions.Logging;
using SartorialWatcher.Core;

namespace SartorialWatcher.Services;

public class SendReportService(
    IScrapingStorage scrapingStorage,
    IReportMessageFactory reportMessageFactory,
    IReportSender reportSender,
    ILogger<SendReportService> logger)
{
    private const string ProductSize = "188-194/40";

    public async Task Invoke()
    {
        var now = DateTimeOffset.Now;
        logger.LogInformation("Requested to send a report");
        var currentProducts = (await scrapingStorage.GetCurrentProductsAsync())
            .Where(product => product.Sizes.Contains(ProductSize)).ToList();
        var newProducts = currentProducts.Where(product => now - product.Timestamp < TimeSpan.FromHours(12));
        var messageContext = new ReportMessageContext(currentProducts.ToList(),
            newProducts.ToList());

        logger.LogDebug("Got relevant {ProductsCount} products", currentProducts.Count);
        var message =
            await reportMessageFactory.CreateMessage(messageContext);
        logger.LogDebug("Created the report message");

        if (string.IsNullOrWhiteSpace(message))
        {
            logger.LogInformation("Report message is empty — skipping sending the report");
            return;
        }

        await reportSender.SendReport(message);
        logger.LogInformation("Sent the report");
    }
}