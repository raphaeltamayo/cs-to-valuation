namespace CStoValuation.Core.Models;

public class PriceHistoryPoint
{
    public int Id { get; set; }

    public string MarketHashName { get; set; } = string.Empty;

    public DateTimeOffset DateUtc { get; set; }

    public decimal Price { get; set; }

    public int? Volume { get; set; }
}
