using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface ICsFloatPriceService
{
    Task<PriceQuote?> GetPriceOverviewAsync(string marketHashName, CancellationToken cancellationToken = default);
}
