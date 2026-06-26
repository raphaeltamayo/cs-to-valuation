using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface ISteamMarketHistoryService
{
    Task<IReadOnlyList<PriceHistoryPoint>> GetPriceHistoryAsync(
        string marketHashName, string currency, CancellationToken cancellationToken = default);
}
