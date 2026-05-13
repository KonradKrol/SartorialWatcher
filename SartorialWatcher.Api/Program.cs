using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Bootstrap;
using SartorialWatcher.Core.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

await builder.Configuration
    .AddAppConfiguration(builder.Environment);

builder.Logging.ClearProviders();

builder.Host.UseSerilog();

builder.Services.AddSartorialWatcher(
    builder.Configuration, builder.Environment);

await using var app = builder.Build();

app.MapEndpoints();
app.Run();