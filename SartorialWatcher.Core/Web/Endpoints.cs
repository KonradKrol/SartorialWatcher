using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;
using SartorialWatcher.Core.Exceptions;
using SartorialWatcher.Core.Services;

namespace SartorialWatcher.Core.Web;

public static class Endpoints
{
    private record ScrapeDto(string ShopName, [Url] string Url);

    public static void MapSartorialWatcherEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok());

        app.MapPost("/scrape", async ([FromBody] ScrapeDto scrapeDto, HttpRequest request,
            IConfiguration configuration, ScrapeShopService scrapeShopService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Http.Scrape.Post");

            using (logger.BeginScope(new Dictionary<string, object> { ["ShopName"] = scrapeDto.ShopName, ["Url"] = scrapeDto.Url }))
            {
                logger.LogInformation("Requested to perform scraping");

                var token = request.Headers["X-Api-Key"];

                if (token != configuration["SchedulerApiKey"])
                {
                    return Results.Unauthorized();
                }

                var scrapingConfiguration = new ScrapingConfiguration
                {
                    ShopName = scrapeDto.ShopName,
                    Url = scrapeDto.Url,
                };

                var products = (await scrapeShopService.Invoke(scrapingConfiguration)).ToList();
                logger.LogInformation("Scraped {ProductsCount}", products.Count);
                return Results.Ok(new { ScrapedProductsCount = products.Count });
            }
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