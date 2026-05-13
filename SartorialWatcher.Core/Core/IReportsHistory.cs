namespace SartorialWatcher.Core.Core;

public class Report
{
    public DateTimeOffset Timestamp { get; set; }
}

public interface IReportsHistory
{
    Task RegisterNewReportAsync(Report report);
    Task<DateTimeOffset?> GetLatestReportDateAsync();
}