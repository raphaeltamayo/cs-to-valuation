using CStoValuation.Core.Enums;

namespace CStoValuation.Core.Models;

public class PriceSnapshot
{
    public int Id { get; set; }

    public string MarketHashName { get; set; } = string.Empty;

    public PriceSource Source { get; set; }

    public decimal? Min { get; set; }

    public decimal? Median { get; set; }

    public decimal? Mean { get; set; }

    public int? Listings { get; set; }

    public int? Volume { get; set; }

    public string Currency { get; set; } = "EUR";

    public DateTimeOffset TakenUtc { get; set; }
}
