using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core;
using SartorialWatcher.Core.Bootstrap;

var builder = Host.CreateApplicationBuilder(args);

await builder.Configuration
    .AddAppConfiguration(builder.Environment);

builder.Configuration.AddUserSecrets<Program>();

builder.Logging.ClearProviders();

builder.Services.AddSartorialWatcher(
    builder.Configuration, builder.Environment);

builder.Services.AddScoped<ConsoleAppRunner>();

using var host = builder.Build();

using var scope = host.Services.CreateScope();

var runner =
    scope.ServiceProvider
        .GetRequiredService<ConsoleAppRunner>();

await runner.RunAsync();