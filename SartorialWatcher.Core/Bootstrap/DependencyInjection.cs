using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using SartorialWatcher.Core.Domain;
using SartorialWatcher.Core.Infrastructure.ReportsHistory;
using SartorialWatcher.Core.Infrastructure.ScrapingConfigurations;
using SartorialWatcher.Core.Infrastructure.Storage;
using SartorialWatcher.Core.Logging;
using SartorialWatcher.Core.Messaging;
using SartorialWatcher.Core.Scrapers;
using SartorialWatcher.Core.Services;
using Serilog;

namespace SartorialWatcher.Core.Bootstrap;

public static class DependencyInjection
{
    public static IServiceCollection AddSartorialWatcher(this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSerilog();

        Log.Logger = environment.IsProduction()
            ? SerilogFactories.CreateProductionLogger(configuration)
            : SerilogFactories.CreateDevelopmentLogger(configuration);

        Console.WriteLine(
            $"Environment: {environment.EnvironmentName}");


        services.AddScoped<VistulaScraper>();
        services.AddScoped<WolczankaScraper>();
        services.AddScoped<BytomScraper>();

        services.AddScoped<IScraperMapper, ScrapersMapper>();

        if (environment.IsProduction())
        {
            var region = configuration["Aws:Region"];
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new InvalidOperationException(
                    "AWS region is missing");
            }

            services.AddSingleton<IAmazonDynamoDB>(_ =>
            {
                var config = new AmazonDynamoDBConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(region)
                };

                return new AmazonDynamoDBClient(config);
            });

            services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient());

            services.AddScoped<IScrapingConfigurations, ProductionConfigurations>(_ =>
                new ProductionConfigurations(wolczankaUrls:
                    [
                        "https://wolczanka.pl/koszule-meskie?sort=PRICE_UP&attributes=132",
                        "https://wolczanka.pl/outlet-koszule-meskie?sort=PRICE_UP",
                        "https://wolczanka.pl/koszule-meskie?attributes=14236,132&sort=PRICE_UP",
                        "https://wolczanka.pl/outlet-dla-niego?attributes=14292&sizes=843,930&sort=PRICE_UP",
                        "https://wolczanka.pl/koszule-meskie?attributes=132"
                    ], bytomUrls:
                    [
                        "https://bytom.com.pl/c-koszule?sort=PRICE_UP&attributes=44,40424,3608,5000,808,328,5164,5276,1424,11049,136",
                        "https://bytom.com.pl/koszule-1818-1?sort=PRICE_UP&attributes=3608,5000,808,328,5164,5276,1424,136"
                    ],
                    vistulaUrls:
                    [
                        "https://vistula.pl/koszule?sort=PRICE_UP&attributes=16876",
                        "https://vistula.pl/promocja?attributes=16876"
                    ]));


            services.AddScoped<IReportSender, TelegramReportSender>();

            services.AddScoped<IReportMessageFactory, TelegramMessageFactory>();

            services.AddScoped<IReportsHistory, DynamoReportsHistory>();
            services.AddScoped<IScrapingStorage, DynamoScrapingStorage>();
        }
        else
        {
            services.AddScoped<IScrapingConfigurations, ProductionConfigurations>(_ =>
                new ProductionConfigurations(wolczankaUrls:
                    ["https://wolczanka.pl/koszule-meskie?attributes=132"]
                    , bytomUrls:
                    [
                        "https://bytom.com.pl/c-koszule?sort=PRICE_UP&attributes=44,40424,3608,5000,808,328,5164,5276,1424,11049,136",
                    ], vistulaUrls:
                    [
                        "https://vistula.pl/koszule?sort=PRICE_UP&attributes=16876",
                        "https://vistula.pl/promocja?attributes=16876"
                    ]));

            services.AddScoped<IReportSender, ConsoleReportSender>();
            // services.AddScoped<IReportSender, TelegramReportSender>();
            services.AddScoped<IReportMessageFactory, TelegramMessageFactory>();

            services.AddScoped<IReportsHistory, InMemoryReportsHistory>();
            services.AddScoped<IScrapingStorage, InMemoryScrapingStorage>();
        }

        services.AddHttpClient();

        services
            .AddHttpClient("scraper")
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(
                [
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                ]));

        services.AddScoped<ScrapeAllShopsService>();
        services.AddScoped<SendReportService>();
        services.AddScoped<ScrapeShopService>();

        return services;
    }
}