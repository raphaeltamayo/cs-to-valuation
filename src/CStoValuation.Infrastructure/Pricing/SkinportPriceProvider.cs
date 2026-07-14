using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.Infrastructure.Pricing;

public sealed class SkinportPriceProvider : IPriceProvider
{
    private readonly ISkinportPriceService _inner;

    public SkinportPriceProvider(ISkinportPriceService inner) => _inner = inner;

    public PriceSource Source => PriceSource.Skinport;

    public Task<IReadOnlyDictionary<string, PriceQuote>> GetAllPricesAsync(
        string currency, CancellationToken cancellationToken = default) =>
        _inner.GetPricesAsync(currency, cancellationToken);
}
