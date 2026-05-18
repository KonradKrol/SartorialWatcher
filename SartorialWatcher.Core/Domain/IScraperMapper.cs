namespace SartorialWatcher.Core.Domain;

public interface IScraperMapper
{
    IScraper GetScraper(string name);
}