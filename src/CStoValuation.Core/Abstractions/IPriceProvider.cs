using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface IPriceProvider
{
    PriceSource Source { get; }

    Task<IReadOnlyDictionary<string, PriceQuote>> GetAllPricesAsync(
        string currency, CancellationToken cancellationToken = default);
}
