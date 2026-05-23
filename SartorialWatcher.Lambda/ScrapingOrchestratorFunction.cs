using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Bootstrap;
using SartorialWatcher.Core.Domain;
using SartorialWatcher.Core.Exceptions;
using SartorialWatcher.Core.Services;

namespace SartorialWatcher.Lambda;

public class ScrapingOrchestratorFunction
{
    private static readonly ServiceProvider ServiceProvider;

    private readonly ILogger<ScrapingOrchestratorFunction> _logger =
        ServiceProvider.GetRequiredService<ILogger<ScrapingOrchestratorFunction>>();

    private readonly IScrapingConfigurations _scrapingConfigurations =
        ServiceProvider.GetRequiredService<IScrapingConfigurations>();

    private readonly IAmazonSQS _sqs = ServiceProvider.GetRequiredService<IAmazonSQS>();

    private readonly string _queueUrl = ServiceProvider.GetRequiredService<IConfiguration>()["Aws:Queue:Url"] ??
                                        throw new InvalidOperationException("Aws:Queue:Url is missing");

    static ScrapingOrchestratorFunction()
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

    public async Task FunctionHandler()
    {
        _logger.LogInformation("Adding scrape jobs to queue");
        var configurations = _scrapingConfigurations.Configurations;
        var jobs = configurations.Select(configuration => new ScrapeJob(configuration.ShopName, configuration.Url));
        _logger.LogDebug("Got {ConfigurationsCount} scraping configurations", configurations.Count);

        foreach (var scrapeJob in jobs)
        {
            await _sqs.SendMessageAsync(
                queueUrl: _queueUrl, messageBody: JsonSerializer.Serialize(scrapeJob));
        }

        _logger.LogInformation("Added {ConfigurationsCount} scrape jobs to the queue", configurations.Count);
    }
}