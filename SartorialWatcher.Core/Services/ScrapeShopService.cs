using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;
using SartorialWatcher.Core.Infrastructure.Storage;

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
            var currentProducts = (await storage.GetCurrentProductsAsync()).ToList();
            var currentPriceById = currentProducts.ToDictionary(product => product.Id, product => product.CurrentPrice);

            logger.LogDebug("Retrieved {CurrentProductsCount} current products", currentProducts.Count);
            var scrapingResult =
                await scraper.ScrapeAsync(new ScrapingContext { Url = new Uri(configuration.Url) });
            logger.LogInformation("Scrapped {ProductsCount} products", scrapingResult.Products.Count);
            var productsToAdd = scrapingResult.Products.Where(ProductIsEligibleToAdd).ToList();
            var skippedProductsCount = scrapingResult.Products.Count - productsToAdd.Count;
            logger.LogInformation(
                "Saving {ProductsCount} new products into the storage. Skipping {SkippedProductsCount} products",
                productsToAdd.Count, skippedProductsCount);
            if (productsToAdd.Count > 0)
            {
                await storage.AddAsync(productsToAdd);
                products.AddRange(productsToAdd);
            }

            return products;

            bool ProductIsEligibleToAdd(ProductSnapshot product)
            {
                var currentPriceExists = currentPriceById.TryGetValue(product.Id, out var currentPrice);
                if (!currentPriceExists) return true;
                return currentPrice != product.CurrentPrice;
            }
        }
    }
}