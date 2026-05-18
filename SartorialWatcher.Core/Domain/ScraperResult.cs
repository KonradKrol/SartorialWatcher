namespace SartorialWatcher.Core.Domain;

public class ScraperResult
{
    public required List<ProductSnapshot> Products { get; set; }
    public required int PagesCount { get; set; }
}