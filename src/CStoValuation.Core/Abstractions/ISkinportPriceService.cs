using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface ISkinportPriceService
{
    Task<IReadOnlyDictionary<string, PriceQuote>> GetPricesAsync(
        string currency, CancellationToken cancellationToken = default);
}
