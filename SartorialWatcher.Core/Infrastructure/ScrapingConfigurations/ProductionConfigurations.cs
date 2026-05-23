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
                ShopName = "Wólczanka",
                Url = url,
            }).Concat(bytomUrls.Select(url => new ScrapingConfiguration()
                {
                    ShopName = "Bytom",
                    Url = url,
                }
            )).Concat(vistulaUrls.Select(url => new ScrapingConfiguration()
            {
                ShopName = "Vistula",
                Url = url,
            })).ToList();
        }
    }
}