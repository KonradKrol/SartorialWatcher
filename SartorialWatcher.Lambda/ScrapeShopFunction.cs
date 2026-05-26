using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Bootstrap;
using SartorialWatcher.Core.Domain;
using SartorialWatcher.Core.Exceptions;
using SartorialWatcher.Core.Services;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer)
)]

namespace SartorialWatcher.Lambda;

public class ScrapeShopFunction
{
    private static readonly ServiceProvider ServiceProvider;

    private readonly ILogger<ScrapeShopFunction> _logger =
        ServiceProvider.GetRequiredService<ILogger<ScrapeShopFunction>>();

    private readonly ScrapeShopService _scrapeShopService = ServiceProvider.GetRequiredService<ScrapeShopService>();

    static ScrapeShopFunction()
    {
        var services = new ServiceCollection();

        var environment = new LambdaHostEnvironment();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .AddAppConfiguration(environment)
            .Build();
        
        services.AddLogging();

        services.AddSartorialWatcher(configuration, environment);
        
        services.AddSingleton<IConfiguration>(configuration);

        ServiceProvider = services.BuildServiceProvider();
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            var body = message.Body;

            var job = JsonSerializer.Deserialize<ScrapeJob>(body);

            if (job == null)
            {
                throw new ScrapeRequestException("Scrape job JSON does not match");
            }

            using (_logger.BeginScope(new Dictionary<string, object> { ["ShopName"] = job.ShopName }))
            {
                _logger.LogInformation("Requested to perform scraping");

                var scrapingConfiguration = new ScrapingConfiguration
                {
                    ShopName = job.ShopName,
                    Url = job.Url,
                };

                var products = (await _scrapeShopService.Invoke(scrapingConfiguration)).ToList();
                _logger.LogInformation("Scraped {ProductsCount} and tried to save", products.Count);
            }
        }
    }
}