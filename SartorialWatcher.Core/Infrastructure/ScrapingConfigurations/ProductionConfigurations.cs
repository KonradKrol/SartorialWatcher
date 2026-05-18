using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Infrastructure.ScrapingConfigurations;

public class ProductionConfigurations(string[] wolczankaUrls, string[] bytomUrls, string[] vistulaUrls)
    : IScrapingConfigurations
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
            )).Concat(vistulaUrls.Select(url => new ScrapingConfiguration()
            {
                ScraperName = "Vistula",
                Url = url,
                IsEnabled = true,
            })).ToList();
        }
    }
}