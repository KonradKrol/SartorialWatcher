using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Infrastructure.Storage;

public class InMemoryScrapingStorage(ILogger<InMemoryScrapingStorage> logger) : IScrapingStorage
{
    private readonly Dictionary<string, ProductSnapshot> _latestProducts = new();

    private readonly List<ProductSnapshot> _productsList = [];

    public Task AddAsync(List<ProductSnapshot> productSnapshots)
    {
        logger.LogInformation("Requested to save scraper result with {ProductsCount} products",
            productSnapshots.Count);

        var newProductsCount = 0;
        foreach (var product in productSnapshots)
        {
            _latestProducts.TryGetValue(product.Id, out var latestProduct);
            if (!ProductIsEligibleToBeAdded(product, latestProduct)) continue;
            _productsList.Add(product);
            _latestProducts[product.Id] = product;
            newProductsCount++;
        }

        logger.LogInformation("Saved {ProductsCount} new products. Skipped {Duplicates}",
            newProductsCount, productSnapshots.Count - newProductsCount);

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<ProductSnapshot>> GetCurrentSnapshotsSinceAsync(DateTimeOffset since)
    {
        return _latestProducts.Values.Where(product => product.Timestamp >= since).ToList();
    }


    private bool ProductIsEligibleToBeAdded(ProductSnapshot product, ProductSnapshot? latestProduct)
    {
        if (latestProduct is null) return true;
        var pricesAreDifferent = product.CurrentPrice != latestProduct.CurrentPrice;
        var timestampIsCorrect = product.Timestamp > latestProduct.Timestamp;
        return pricesAreDifferent && timestampIsCorrect;
    }

    public Task<ProductSnapshot?> GetLatestSnapshotAsync(string productId)
    {
        return Task.FromResult(_latestProducts.GetValueOrDefault(productId));
    }

    public async Task<IReadOnlyCollection<ProductSnapshot>> GetCurrentProductsAsync()
    {
        return _latestProducts.Values.ToList();
    }

    public async Task<ProductSnapshot> FindCheapestSnapshotAsync(string productId)
    {
        return _productsList.Where(product => product.Id == productId).MinBy(product => product.CurrentPrice) ??
               throw new InvalidOperationException($"Not found the product with ID {productId}");
    }
}