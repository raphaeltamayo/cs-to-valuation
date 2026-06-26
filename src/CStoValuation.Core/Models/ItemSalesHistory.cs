namespace CStoValuation.Core.Models;

public sealed record SalesWindow
{
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public decimal? Average { get; init; }
    public decimal? Median { get; init; }
    public int Volume { get; init; }

    public static SalesWindow Empty { get; } = new();
}

public sealed record ItemSalesHistory
{
    public required string MarketHashName { get; init; }
    public required string Currency { get; init; }
    public SalesWindow Last24Hours { get; init; } = SalesWindow.Empty;
    public SalesWindow Last7Days { get; init; } = SalesWindow.Empty;
    public SalesWindow Last30Days { get; init; } = SalesWindow.Empty;
    public SalesWindow Last90Days { get; init; } = SalesWindow.Empty;
}
