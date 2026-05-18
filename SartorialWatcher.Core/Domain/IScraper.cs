namespace SartorialWatcher.Core.Domain;

public interface IScraper
{
    Task<ScraperResult> ScrapeAsync(ScrapingContext context);
}