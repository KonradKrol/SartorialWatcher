using SartorialWatcher.Core.Core;

namespace SartorialWatcher.Core.Messaging;

public class ConsoleReportSender : IReportSender
{
    public Task SendReport(string message)
    {
        Console.WriteLine($"### RAPORT ###:\n{message}");
        return Task.CompletedTask;
    }
}