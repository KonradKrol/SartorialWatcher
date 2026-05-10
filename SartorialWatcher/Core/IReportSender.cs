namespace SartorialWatcher.Core;

public interface IReportSender
{
    Task SendReport(string message);
}