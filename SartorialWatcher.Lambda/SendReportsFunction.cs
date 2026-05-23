using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Bootstrap;
using SartorialWatcher.Core.Services;

namespace SartorialWatcher.Lambda;

public class SendReportsFunction
{
    private static readonly ServiceProvider ServiceProvider;

    private readonly ILogger<SendReportsFunction> _logger =
        ServiceProvider.GetRequiredService<ILogger<SendReportsFunction>>();

    private readonly SendReportService _sendReportService = ServiceProvider.GetRequiredService<SendReportService>();

    static SendReportsFunction()
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
        _logger.LogInformation("Requested to send reports");

        var sent = await _sendReportService.Invoke();

        _logger.LogInformation("Tried to send reports. Result: {HasSent}", sent);
    }
}