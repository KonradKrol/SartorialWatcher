using SartorialWatcher.Core.Core;

namespace SartorialWatcher.Core.Infrastructure.ScrapingConfigurations;

public class WolczankaAndBytom(string[] wolczankaUrls, string[] bytomUrls) : IScrapingConfigurations
{
    public List<ScrapingConfiguration> Configurations
    {
        get
        {
            return wolczankaUrls.Select(url => new ScrapingConfiguration()
            {
                ScraperName = "Wólczanka",
                Url = url,
                IsEnabled = true
            }).Concat(bytomUrls.Select(url => new ScrapingConfiguration()
                {
                    ScraperName = "Bytom",
                    Url = url,
                    IsEnabled = true
                }
            )).ToList();
        }
    }
}