using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Services;

public class ScrapeAllShopsService(
    IScrapingStorage storage,
    IScrapingConfigurations scrapingConfigurations,
    IScraperMapper scraperMapper,
    ILogger<ScrapeAllShopsService> logger)
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
                       ["ShopName"] = configuration.ShopName,
                       ["Url"] = configuration.Url
                   }))
            {
                var scraper = scraperMapper.GetScraper(configuration.ShopName);
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

        products = products.ToHashSet().ToList();

        logger.LogInformation("Scraped {ProductsCount} products", products.Count);

        return products;
    }
}