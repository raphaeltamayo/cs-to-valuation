namespace CStoValuation.Core.Models;

/// <summary>
/// A daily point-in-time capture of the whole portfolio's value, feeding the performance
/// chart. Unlike <see cref="PriceSnapshot"/> (one row per item, every 15 minutes), this is one
/// row per day for the portfolio as a whole.
/// </summary>
public class PortfolioSnapshot
{
    public int Id { get; set; }

    public decimal TotalGross { get; set; }

    public decimal TotalNet { get; set; }

    public string Currency { get; set; } = "EUR";

    public DateTimeOffset TakenUtc { get; set; }
}
