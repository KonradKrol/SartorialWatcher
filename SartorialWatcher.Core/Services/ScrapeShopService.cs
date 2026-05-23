using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Services;

public class ScrapeShopService(
    IScrapingStorage storage,
    IScraperMapper scraperMapper,
    ILogger<ScrapeAllShopsService> logger)
{
    public async Task<IEnumerable<ProductSnapshot>> Invoke(ScrapingConfiguration configuration)
    {
        var products = new List<ProductSnapshot>();

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["ShopName"] = configuration.ShopName,
                   ["Url"] = configuration.Url
               }))
        {
            logger.LogInformation("Requested to scrape a shop");
            var scraper = scraperMapper.GetScraper(configuration.ShopName);
            logger.LogDebug("Got {ScraperType} implementation",
                nameof(scraper));
            var scrapingResult =
                await scraper.ScrapeAsync(new ScrapingContext { Url = new Uri(configuration.Url) });
            logger.LogInformation("Scrapped {ProductsCount} products", scrapingResult.Products.Count);
            await storage.AddAsync(scrapingResult.Products);
            logger.LogInformation("Saved products in storage");
            products.AddRange(scrapingResult.Products);

            return products;
        }
    }
}