namespace SartorialWatcher.Core.Domain;

public interface IScrapingStorage
{
    /// Does not add duplicates
    Task AddAsync(List<ProductSnapshot> productSnapshots);

    /// Returns the latest snapshot for each product created on or after the specified point in time.
    /// 
    /// If multiple snapshots exist for the same product within the time window,
    /// only the most recent snapshot is returned.
    Task<IReadOnlyCollection<ProductSnapshot>> GetCurrentSnapshotsSinceAsync(DateTimeOffset since);
    
    Task<ProductSnapshot?> GetLatestSnapshotAsync(string productId);
    Task<IReadOnlyCollection<ProductSnapshot>> GetCurrentProductsAsync();
    Task<ProductSnapshot?> FindCheapestSnapshotAsync(string productId);
}