namespace SartorialWatcher.Core;

public record ReportMessageContext(List<ProductSnapshot> Products, List<ProductSnapshot> ProductsAddedSinceLastReport);

public interface IReportMessageFactory
{
    Task<string> CreateMessage(ReportMessageContext context);
}