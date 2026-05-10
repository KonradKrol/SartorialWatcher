using Microsoft.Extensions.Logging;
using SartorialWatcher.Core;

namespace SartorialWatcher.Services;

// TODO: Co z faktem, że nie znamy nowych produktów? Nie wiemy, jakie były duplikaty.
public class PerformScrapingService(
    IScrapingStorage storage,
    IScrapingConfigurations scrapingConfigurations,
    IScraperMapper scraperMapper,
    // INewDealsTracker newDealsTracker,
    ILogger<PerformScrapingService> logger)
{
    public async Task<IEnumerable<ProductSnapshot>> Invoke()
    {
        logger.LogInformation("Requested to perform scraping");
        var configurations = scrapingConfigurations.Configurations;

        logger.LogInformation("Loaded {ConfigurationsCount} scraping configurations", configurations.Count);

        var products = new List<ProductSnapshot>();

        foreach (var configuration in configurations)
        {
            using (logger.BeginScope(new Dictionary<string, object>
                   {
                       ["ScraperName"] = configuration.ScraperName,
                       ["Url"] = configuration.Url
                   }))
            {
                var scraper = scraperMapper.GetScraper(configuration.ScraperName);
                logger.LogDebug("Got {ScraperType} implementation",
                    nameof(scraper));
                var scrapingResult =
                    await scraper.ScrapeAsync(new ScrapingContext { Url = new Uri(configuration.Url) });
                logger.LogInformation("Scrapped {ProductsCount} products", scrapingResult.Products.Count);
                await storage.AddAsync(scrapingResult.Products);
                logger.LogInformation("Saved products");
                products.AddRange(scrapingResult.Products);
            }
        }

        logger.LogInformation("Scraped {ProductsCount} new products", products.Count);

        return products;
    }
}