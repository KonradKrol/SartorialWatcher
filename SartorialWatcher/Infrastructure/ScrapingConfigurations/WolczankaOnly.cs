using SartorialWatcher.Core;

namespace SartorialWatcher.Infrastructure.ScrapingConfigurations;

public class WolczankaOnly(string[] urls) : IScrapingConfigurations
{
    public List<ScrapingConfiguration> Configurations
    {
        get
        {
            return urls.Select(url => new ScrapingConfiguration()
            {
                ScraperName = "Wólczanka",
                Url = url,
                IsEnabled = true
            }).ToList();
        }
    }
}