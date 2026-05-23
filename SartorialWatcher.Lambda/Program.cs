using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Bootstrap;
using SartorialWatcher.Core.Exceptions;
using SartorialWatcher.Core.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

await builder.Configuration
    .AddAppConfigurationAsync(builder.Environment);

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

app.Use(async (httpContext, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        var problemDetails = exception switch
        {
            ScraperNotFoundException scraperNotFoundException => new ProblemDetails()
            {
                Title = "Scraper not found",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Scraper for shop {scraperNotFoundException.ShopName} not found",
                Instance = httpContext.Request.Path,
            },
            ScrapeRequestException scrapeRequestException => new ProblemDetails()
            {
                Title = "Scrape request failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = scrapeRequestException.Message,
                Instance = httpContext.Request.Path
            },
            MessageTooLongException or _ =>
                new ProblemDetails
                {
                    Title = "An error occured",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "Please report the error.",
                    Instance = httpContext.Request.Path,
                }
        };

        problemDetails.Type = $"https://httpstatuses.com/{problemDetails.Status}";
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext()
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
});

app.MapSartorialWatcherEndpoints();
app.Run();