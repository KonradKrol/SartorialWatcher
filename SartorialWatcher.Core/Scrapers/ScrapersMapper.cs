using SartorialWatcher.Core.Core;

namespace SartorialWatcher.Core.Scrapers;

public class ScrapersMapper(HttpClient http) : IScraperMapper
{
    public IScraper GetScraper(string name)
    {
        return name switch
        {
            "Wólczanka" => new WolczankaScraper(http),
            "Bytom" => new BytomScraper(http),
            "Mocked" => new MockedScraper(),
            _ => throw new NotImplementedException($"Scraper {name} is not implemented yet.")
        };
    }
}