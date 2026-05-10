namespace SartorialWatcher.Core;

public interface IScraper
{
    Task<ScraperResult> ScrapeAsync(ScrapingContext context);
}