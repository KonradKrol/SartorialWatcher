using Amazon;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SartorialWatcher.Core.Core;
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
        // services.AddLogging();
        
        services.AddSerilog();
        
        Log.Logger = environment.IsProduction() ? SerilogFactories.CreateProductionLogger(configuration) : SerilogFactories.CreateDevelopmentLogger(configuration);

        Console.WriteLine(
            $"Environment: {environment.EnvironmentName}");

        services.AddScoped<IScrapingConfigurations, WolczankaAndBytom>(_ => new WolczankaAndBytom(wolczankaUrls:
            [
                // "https://wolczanka.pl/koszule-meskie?sort=PRICE_UP&attributes=132&sizes=843,930&priceTo=120",
                // "https://wolczanka.pl/outlet-koszule-meskie?sort=PRICE_UP&sizes=843,930&priceTo=120",
                // "https://wolczanka.pl/koszule-meskie?attributes=14236,240,132&sizes=930&priceTo=301",
                // "https://wolczanka.pl/outlet-dla-niego?attributes=14292&sizes=843,930&sort=PRICE_UP"
            ], bytomUrls:
            [
                "https://bytom.com.pl/c-koszule?sort=PRICE_UP&attributes=44,40424,3608,5000,808,328,5164,5276,1424,11049&sizes=999&priceTo=270&occasion=1",
                "https://bytom.com.pl/koszule-1818-1?sort=PRICE_UP&attributes=3608,5000,808,328,5164,5276,1424,11049&sizes=998,999"
            ]));

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

            services.AddScoped<IScraperMapper, ScrapersMapper>();
            services.AddScoped<IReportSender, TelegramReportSender>();

            services.AddScoped<IReportMessageFactory, TelegramMessageFactory>();

            services.AddScoped<IReportsHistory, DynamoReportsHistory>();
            services.AddScoped<IScrapingStorage, DynamoScrapingStorage>();
            
            services.AddAWSLambdaHosting(
                LambdaEventSource.RestApi);
        }
        else
        {
            services.AddScoped<IScraperMapper, ScrapersMapper>();
            services.AddScoped<IReportSender, ConsoleReportSender>();

            services.AddScoped<IReportMessageFactory, TelegramMessageFactory>();
            
            services.AddScoped<IReportsHistory, InMemoryReportsHistory>();
            services.AddScoped<IScrapingStorage, InMemoryScrapingStorage>();
        }
        
        services.AddHttpClient();

        services.AddScoped<PerformScrapingService>();
        services.AddScoped<SendReportService>();
        
        return services;
    }
}