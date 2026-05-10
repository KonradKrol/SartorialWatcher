namespace SartorialWatcher.Core;

public interface IScraperMapper
{
    IScraper GetScraper(string name);
}