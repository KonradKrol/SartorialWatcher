using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Services;

public class SendReportService(
    IScrapingStorage scrapingStorage,
    IReportMessageFactory reportMessageFactory,
    IReportSender reportSender,
    IReportsHistory reportsHistory,
    ILogger<SendReportService> logger)
{
    private const string ProductSize = "188-194/40";

    public async Task<bool> Invoke()
    {
        logger.LogInformation("Requested to send a report");
        var lastReportBeenAt = await reportsHistory.GetLatestReportDateAsync() ?? DateTimeOffset.MinValue;
        logger.LogDebug("Retrieved last report date which is {LastReportDate}", lastReportBeenAt);
        var currentProducts = (await scrapingStorage.GetCurrentProductsAsync())
            .Where(product => product.Sizes.Contains(ProductSize)).ToList();
        var newProducts = currentProducts.Where(product => product.Timestamp >= lastReportBeenAt).ToList();

        if (newProducts.Count == 0)
        {
            logger.LogInformation("No new products — skipping sending the report");
            return false;
        }
        
        var messageContext = new ReportMessageContext(currentProducts.ToList(),
            newProducts.ToList());

        logger.LogDebug("Got relevant {ProductsCount} products", currentProducts.Count);
        var message =
            await reportMessageFactory.CreateMessage(messageContext);
        logger.LogDebug("Created the report message");

        if (string.IsNullOrWhiteSpace(message))
        {
            logger.LogInformation("Report message is empty — skipping sending the report");
            return false;
        }

        var now = DateTimeOffset.Now;
        var sent = await reportSender.SendReport(message);
        if (!sent)
        {
            return false;
        }
        await reportsHistory.RegisterNewReportAsync(new Report { Timestamp = now });
        logger.LogInformation("Sent the report");
        return true;
    }
}

// chcemy itemy które pojawiły się po lastReportBeenAt