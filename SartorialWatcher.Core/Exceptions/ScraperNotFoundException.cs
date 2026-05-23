namespace SartorialWatcher.Core.Exceptions;

public class ScraperNotFoundException(string shopName) : Exception($"Scraper {shopName} not found")
{
    public string ShopName { get; } = shopName;
}