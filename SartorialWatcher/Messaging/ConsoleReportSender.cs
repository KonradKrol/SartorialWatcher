using SartorialWatcher.Core;

namespace SartorialWatcher.Messaging;

public class ConsoleReportSender : IReportSender
{
    public Task SendReport(string message)
    {
        Console.WriteLine($"### RAPORT ###:\n{message}");
        return Task.CompletedTask;
    }
}