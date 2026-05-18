using Microsoft.Extensions.DependencyInjection;
using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Scrapers;

public class ScrapersMapper(HttpClient http, IServiceProvider serviceProvider) : IScraperMapper
{
    public IScraper GetScraper(string name)
    {
        return name switch
        {
            "Wólczanka" => serviceProvider.GetRequiredService<WolczankaScraper>(),
            "Bytom" => serviceProvider.GetRequiredService<BytomScraper>(),
            "Vistula" => serviceProvider.GetRequiredService<VistulaScraper>(),
            "Mocked" => new MockedScraper(),
            _ => throw new NotImplementedException($"Scraper {name} is not implemented yet.")
        };
    }
}