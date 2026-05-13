namespace SartorialWatcher.Core.Core;

public class ScrapingConfiguration
{
    public required string ScraperName { get; set; }
    public required string Url { get; set; }
    public bool IsEnabled { get; set; }
}