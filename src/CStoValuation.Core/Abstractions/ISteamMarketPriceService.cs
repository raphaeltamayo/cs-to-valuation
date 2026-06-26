using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface ISteamMarketPriceService
{
    Task<PriceQuote?> GetPriceOverviewAsync(
        string marketHashName, string currency, CancellationToken cancellationToken = default);
}
