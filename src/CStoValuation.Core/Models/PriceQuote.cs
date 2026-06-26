using CStoValuation.Core.Enums;

namespace CStoValuation.Core.Models;

public sealed record PriceQuote
{
    public required string MarketHashName { get; init; }

    public PriceSource Source { get; init; }

    public required string Currency { get; init; }

    public decimal Gross { get; init; }

    public int? Listings { get; init; }

    public int? Volume { get; init; }

    public DateTimeOffset AsOfUtc { get; init; }
}
