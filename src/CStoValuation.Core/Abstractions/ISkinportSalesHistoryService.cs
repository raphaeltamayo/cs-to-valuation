using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface ISkinportSalesHistoryService
{
    Task<IReadOnlyDictionary<string, ItemSalesHistory>> GetSalesHistoryAsync(
        string currency, CancellationToken cancellationToken = default);
}
