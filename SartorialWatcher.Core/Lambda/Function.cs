using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using SartorialWatcher.Core.Services;

namespace SartorialWatcher.Core.Lambda;

public sealed class Function
{
    private readonly IServiceProvider _services;

    public Function()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        _services = services.BuildServiceProvider();
    }

    public async Task FunctionHandler(
        SchedulerRequest request,
        ILambdaContext context)
    {
        using var scope = _services.CreateScope();

        switch (request.Action)
        {
            case "scrape":
            {
                var performScraping =
                    scope.ServiceProvider
                        .GetRequiredService<PerformScrapingService>();

                await performScraping.Invoke();

                break;
            }

            case "send_reports":
            {
                var sendReport =
                    scope.ServiceProvider
                        .GetRequiredService<SendReportService>();

                await sendReport.Invoke();

                break;
            }

            default:
                throw new InvalidOperationException(
                    $"Unknown action: {request.Action}");
        }
    }

    private static void ConfigureServices(
        IServiceCollection services)
    {
        services.AddLogging();

        services.AddScoped<PerformScrapingService>();
        services.AddScoped<SendReportService>();

        // etc
    }
}