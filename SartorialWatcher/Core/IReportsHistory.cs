namespace SartorialWatcher.Core;

public interface IReportsHistory
{
    Task RegisterNewReportAsync(DateTimeOffset dateTimeOffset);
    Task<DateTimeOffset> GetLatestReportDateAsync();
}