using System.Diagnostics;
using SartorialWatcher.Core.Services;

namespace SartorialWatcher.Console;

public class ConsoleAppRunner(ScrapeAllShopsService scrapeAllShopsService, SendReportService sendReportService)
{
    public async Task RunAsync()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var newProducts = await scrapeAllShopsService.Invoke();
        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed.TotalSeconds;
        System.Console.WriteLine($"Wywołanie scrapera zajęło {elapsed}s");

        stopwatch.Restart();
        await sendReportService.Invoke();
        stopwatch.Stop();
        elapsed = stopwatch.Elapsed.TotalSeconds;
        System.Console.WriteLine($"Wysłanie raportu zajęło {elapsed}s");
    }
}