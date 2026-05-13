using System.Diagnostics;
using SartorialWatcher.Core.Services;

namespace SartorialWatcher.Core;

public class ConsoleAppRunner(PerformScrapingService performScrapingService, SendReportService sendReportService)
{
    public async Task RunAsync()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var newProducts = await performScrapingService.Invoke();
        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Wywołanie scrapera zajęło {elapsed}s");

        stopwatch.Restart();
        await sendReportService.Invoke();
        stopwatch.Stop();
        elapsed = stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Wysłanie raportu zajęło {elapsed}s");
    }
}