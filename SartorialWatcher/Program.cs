using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SartorialWatcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core;
using SartorialWatcher.Infrastructure.ReportsHistory;
using SartorialWatcher.Infrastructure.ScrapingConfigurations;
using SartorialWatcher.Infrastructure.Storage;
using SartorialWatcher.Logging;
using SartorialWatcher.Messaging;
using SartorialWatcher.Scrapers;
using SartorialWatcher.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
// builder.Logging.ClearProviders();
// builder.Logging.AddConsole();

Log.Logger = SerilogFactories.CreateLogger(builder.Configuration);

builder.Host.UseSerilog();

builder.Services.AddSingleton<AppRunner>();
Console.WriteLine(
    $"Environment: {builder.Environment.EnvironmentName}");
if (builder.Environment.IsProduction())
{
    var secretName =
        Environment.GetEnvironmentVariable("SECRET_NAME")
        ?? throw new InvalidOperationException();

    var secretsClient = new AmazonSecretsManagerClient();

    var secretResponse =
        await secretsClient.GetSecretValueAsync(
            new GetSecretValueRequest
            {
                SecretId = secretName
            });

    var secrets =
        JsonSerializer.Deserialize<Dictionary<string, string>>(
            secretResponse.SecretString!)
        ?? throw new InvalidOperationException("Secrets deserialization failed");

    builder.Configuration.AddInMemoryCollection(secrets);
}

builder.Configuration.AddUserSecrets<Program>();

var region = builder.Configuration["Aws:Region"];
if (string.IsNullOrWhiteSpace(region))
{
    throw new InvalidOperationException(
        "AWS region is missing");
}

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var config = new AmazonDynamoDBConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(region)
    };

    return new AmazonDynamoDBClient(config);
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

builder.Services.AddScoped<IReportsHistory, DynamoReportsHistory>();
builder.Services.AddScoped<IScrapingStorage, DynamoScrapingStorage>();
builder.Services.AddScoped<PerformScrapingService>();
builder.Services.AddScoped<SendReportService>();

builder.Services.AddHttpClient();

builder.Services.AddAWSLambdaHosting(
    LambdaEventSource.RestApi);

await using var host = builder.Build();

host.MapGet("/health", () => Results.Ok());

host.MapPost("/scrape", async (HttpRequest request,
    IConfiguration configuration, PerformScrapingService performScrapingService, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("Http.Scrape.Post");

    logger.LogInformation("Requested to perform scraping");

    // logger.LogInformation("Path: {Path}", request.Path);
    // logger.LogInformation("Method: {Method}", request.Method);
    // logger.LogInformation(
    //     "Source IP: {SourceIp}",
    //     request.HttpContext.Connection.RemoteIpAddress);

    var headers = request.Headers.ToDictionary(
        x => x.Key,
        x => x.Value.ToString());

    // logger.LogInformation(
    //     "Headers: {Headers}",
    //     JsonSerializer.Serialize(headers));

    var token = request.Headers["X-Api-Key"];

    if (token != configuration["SchedulerApiKey"])
    {
        return Results.Unauthorized();
    }

    var products = (await performScrapingService.Invoke()).ToList();
    logger.LogInformation("Scraped {ProductsCount}", products.Count);
    return Results.Ok(new { ScrapedProductsCount = products.Count });
});

host.MapPost("/send_reports", async (HttpRequest request,
    IConfiguration configuration, SendReportService sendReportService, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("Http.SendReports.Post");
    logger.LogInformation("Requested to send the report");

    var token = request.Headers["X-Api-Key"];

    if (token != configuration["SchedulerApiKey"])
    {
        return Results.Unauthorized();
    }

    var sent = await sendReportService.Invoke();
    logger.LogInformation("Tried to sent the report. Result: {HasSent}", sent);
    return Results.Ok(new { Sent = sent });
});

host.Run();