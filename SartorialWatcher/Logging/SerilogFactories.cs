using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace SartorialWatcher.Logging;

public static class SerilogFactories
{
    public static Logger CreateLogger(IConfiguration configuration)
    {
        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();
    }
}