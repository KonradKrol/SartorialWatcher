namespace SartorialWatcher.Core.Core;

public interface IScraperMapper
{
    IScraper GetScraper(string name);
}