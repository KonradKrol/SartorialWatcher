using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Messaging;

public class ConsoleReportSender : IReportSender
{
    public Task<bool> SendReport(string message)
    {
        Console.WriteLine($"### RAPORT ###:\n{message}");
        return Task.FromResult(true);
    }
}