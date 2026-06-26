namespace CStoValuation.Core.Models;

public sealed record InventoryValuation
{
    public required IReadOnlyList<ValuedItem> Items { get; init; }

    public decimal TotalGross { get; init; }

    public decimal TotalNet { get; init; }

    public required string Currency { get; init; }

    public int PricedCount { get; init; }

    public int UnpricedCount { get; init; }

    public static InventoryValuation Empty(string currency) => new()
    {
        Items = [],
        Currency = currency,
        TotalGross = 0m,
        TotalNet = 0m,
        PricedCount = 0,
        UnpricedCount = 0,
    };
}
