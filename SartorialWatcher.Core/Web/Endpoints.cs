using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Services;


namespace SartorialWatcher.Core.Web;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok());

        app.MapPost("/scrape", async (HttpRequest request,
            IConfiguration configuration, PerformScrapingService performScrapingService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Http.Scrape.Post");

            logger.LogInformation("Requested to perform scraping");

            // logger.LogInformation("Path: {Path}", request.Path);
            // logger.LogInformation("Method: {Method}", request.Method);
            // logger.LogInformation(
            //     "Source IP: {SourceIp}",
            //     request.HttpContext.Connection.RemoteIpAddress);

            var headers = request.Headers.ToDictionary(
                x => x.Key,
                x => x.Value.ToString());

            // logger.LogInformation(
            //     "Headers: {Headers}",
            //     JsonSerializer.Serialize(headers));

            var token = request.Headers["X-Api-Key"];

            if (token != configuration["SchedulerApiKey"])
            {
                return Results.Unauthorized();
            }

            var products = (await performScrapingService.Invoke()).ToList();
            logger.LogInformation("Scraped {ProductsCount}", products.Count);
            return Results.Ok(new { ScrapedProductsCount = products.Count });
        });

        app.MapPost("/send_reports", async (HttpRequest request,
            IConfiguration configuration, SendReportService sendReportService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Http.SendReports.Post");
            logger.LogInformation("Requested to send the report");

            var token = request.Headers["X-Api-Key"];

            if (token != configuration["SchedulerApiKey"])
            {
                return Results.Unauthorized();
            }

            var sent = await sendReportService.Invoke();
            logger.LogInformation("Tried to sent the report. Result: {HasSent}", sent);
            return Results.Ok(new { Sent = sent });
        });
    }
}