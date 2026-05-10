using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core;
using SartorialWatcher.Messaging;
using SartorialWatcher.Services;

namespace SartorialWatcher;

// normal 188-194/40, MAX 100zł: https://wolczanka.pl/koszule-meskie?sort=PRICE_UP&attributes=132&sizes=843,930&priceTo=100
// outlet MAX 140zł: https://wolczanka.pl/outlet-koszule-meskie?sort=PRICE_UP&sizes=843,930&priceTo=140
public class AppRunner(PerformScrapingService performScrapingService, SendReportService sendReportService)
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