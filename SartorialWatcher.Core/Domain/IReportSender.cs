namespace SartorialWatcher.Core.Domain;

public interface IReportSender
{
    Task<bool> SendReport(string message);
}