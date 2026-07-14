using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.Tests.TestSupport;

internal sealed class FakePriceProvider : IPriceProvider
{
    private readonly IReadOnlyDictionary<string, PriceQuote> _prices;

    public FakePriceProvider(PriceSource source, IReadOnlyDictionary<string, PriceQuote> prices)
    {
        Source = source;
        _prices = prices;
    }

    public PriceSource Source { get; }

    public int CallCount { get; private set; }

    public Task<IReadOnlyDictionary<string, PriceQuote>> GetAllPricesAsync(
        string currency, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(_prices);
    }
}
