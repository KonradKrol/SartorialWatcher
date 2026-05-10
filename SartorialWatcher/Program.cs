using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using SartorialWatcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core;
using SartorialWatcher.Infrastructure.ScrapingConfigurations;
using SartorialWatcher.Infrastructure.Storage;
using SartorialWatcher.Messaging;
using SartorialWatcher.Scrapers;
using SartorialWatcher.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<AppRunner>();

builder.Configuration.AddUserSecrets<Program>();

var awsSection = builder.Configuration.GetSection("Aws");
var accessKey = awsSection["AccessKey"];
var secretKey = awsSection["SecretKey"];
var region = awsSection["Region"];
if (string.IsNullOrWhiteSpace(region))
{
    throw new InvalidOperationException(
        "AWS region is missing.");
}
builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var credentials =
        new BasicAWSCredentials(accessKey, secretKey);

    var config = new AmazonDynamoDBConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(region)
    };

    return new AmazonDynamoDBClient(credentials, config);
});

builder.Services.AddScoped<IScraper, WolczankaScraper>();
builder.Services.AddScoped<IScraperMapper, ScrapersMapper>();
builder.Services.AddScoped<IReportSender, TelegramReportSender>();
// builder.Services.AddScoped<IReportSender, ConsoleReportSender>();
builder.Services.AddScoped<IReportMessageFactory, TelegramMessageFactory>();
builder.Services.AddScoped<IScrapingConfigurations, WolczankaOnly>(_ =>
    new WolczankaOnly(urls:
    [
        "https://wolczanka.pl/koszule-meskie?sort=PRICE_UP&attributes=132&sizes=843,930&priceTo=120",
        "https://wolczanka.pl/outlet-koszule-meskie?sort=PRICE_UP&sizes=843,930&priceTo=120",
        "https://wolczanka.pl/koszule-meskie?attributes=14236,240,132&sizes=930&priceTo=301",
        "https://wolczanka.pl/outlet-dla-niego?attributes=14292&sizes=843,930&sort=PRICE_UP"
    ]));
// builder.Services.AddScoped<IScrapingStorage, InMemoryStorage>();

builder.Services.AddScoped<IScrapingStorage, DynamoScrapingStorage>();
builder.Services.AddScoped<PerformScrapingService>();
builder.Services.AddScoped<SendReportService>();

builder.Services.AddLogging();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpClient();

using var host = builder.Build();

await host.Services.GetRequiredService<AppRunner>().RunAsync();