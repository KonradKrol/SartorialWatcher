namespace SartorialWatcher.Core.Core;

public interface IReportSender
{
    Task SendReport(string message);
}