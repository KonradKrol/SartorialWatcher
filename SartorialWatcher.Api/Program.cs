using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
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

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    const string headerName = "X-Correlation-Id";

    var correlationId = Activity.Current?.TraceId.ToString()
                        ?? context.TraceIdentifier;

    // var correlationId = context.Request.Headers.TryGetValue(headerName, out var existing)
    //                     && !string.IsNullOrWhiteSpace(existing)
    //     ? existing.ToString()
    //     : Guid.NewGuid().ToString();

    // context.Items[headerName] = correlationId;
    context.Response.Headers[headerName] = correlationId;

    using (logger.BeginScope(new Dictionary<string, object>
           {
               ["CorrelationId"] = correlationId
           }))
    {
        await next();
    }
});

app.MapEndpoints();
app.Run();