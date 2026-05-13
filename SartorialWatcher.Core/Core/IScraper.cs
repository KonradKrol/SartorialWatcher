namespace SartorialWatcher.Core.Core;

public interface IScraper
{
    Task<ScraperResult> ScrapeAsync(ScrapingContext context);
}