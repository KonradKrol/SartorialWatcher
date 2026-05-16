namespace SartorialWatcher.Core.Core;

public interface IReportSender
{
    Task<bool> SendReport(string message);
}