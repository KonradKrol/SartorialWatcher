using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Infrastructure.Storage;

public class InMemoryCacheScrapingStorage(
    IScrapingStorage nestedScrapingStorage,
    InMemoryCacheSettings inMemoryCacheSettings) : IScrapingStorage
{
    private CachedProductsList? _cachedProducts;
    private readonly Dictionary<DateTimeOffset, CachedProductsList> _cachedProductsBySinceDate = new();

    public Task AddAsync(List<ProductSnapshot> productSnapshots)
    {
        return nestedScrapingStorage.AddAsync(productSnapshots);
    }

    public async Task<IReadOnlyCollection<ProductSnapshot>> GetCurrentSnapshotsSinceAsync(DateTimeOffset since)
    {
        var validCacheFound = _cachedProductsBySinceDate.TryGetValue(since, out var cachedProductsList) &&
                              ProductsAreValid(
                                  cachedProductsList,
                                  DateTimeOffset.Now, inMemoryCacheSettings.productsSinceTtl);
        if (validCacheFound)
        {
            return cachedProductsList!.Products;
        }

        var retrievedProducts = await nestedScrapingStorage.GetCurrentSnapshotsSinceAsync(since);
        _cachedProductsBySinceDate[since] = new CachedProductsList(DateTimeOffset.Now, retrievedProducts.ToList());
        return retrievedProducts;
    }

    public Task<ProductSnapshot?> GetLatestSnapshotAsync(string productId)
    {
        return nestedScrapingStorage.GetLatestSnapshotAsync(productId);
    }

    public async Task<IReadOnlyCollection<ProductSnapshot>> GetCurrentProductsAsync()
    {
        var validCacheFound = _cachedProducts is not null && ProductsAreValid(
            _cachedProducts,
            DateTimeOffset.Now, inMemoryCacheSettings.productsTtl);
        if (validCacheFound)
        {
            return _cachedProducts!.Products;
        }

        var retrievedProducts = await nestedScrapingStorage.GetCurrentProductsAsync();
        _cachedProducts = new CachedProductsList(DateTimeOffset.Now, retrievedProducts.ToList());
        return retrievedProducts;
    }

    public Task<ProductSnapshot?> FindCheapestSnapshotAsync(string productId)
    {
        return nestedScrapingStorage.GetLatestSnapshotAsync(productId);
    }

    private bool ProductsAreValid(CachedProductsList cachedProductsList, DateTimeOffset now, TimeSpan ttl)
    {
        return cachedProductsList.CreatedAt + ttl < now;
    }
}

public record InMemoryCacheSettings(TimeSpan productsTtl, TimeSpan productsSinceTtl);

internal record CachedProductsList(DateTimeOffset CreatedAt, List<ProductSnapshot> Products);