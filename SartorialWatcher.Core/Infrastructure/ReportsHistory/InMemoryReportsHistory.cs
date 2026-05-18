using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Infrastructure.ReportsHistory;

public class InMemoryReportsHistory : IReportsHistory
{
    private readonly List<DateTimeOffset> _history = [];

    public Task RegisterNewReportAsync(Report report)
    {
        _history.Add(report.Timestamp);
        return Task.CompletedTask;
    }

    public Task<DateTimeOffset?> GetLatestReportDateAsync()
    {
        return Task.FromResult<DateTimeOffset?>(_history.LastOrDefault());
    }
}