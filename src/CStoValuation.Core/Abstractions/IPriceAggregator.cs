using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface IPriceAggregator
{
    Task<IReadOnlyDictionary<string, PriceQuote>> GetPrimaryPricesAsync(
        string currency, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<PriceSource, PriceQuote>> GetAllSourceQuotesAsync(
        string marketHashName, string currency, CancellationToken cancellationToken = default);
}
