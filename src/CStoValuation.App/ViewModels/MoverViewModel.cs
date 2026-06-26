using CStoValuation.App.Presentation;

namespace CStoValuation.App.ViewModels;

/// <summary>
/// One ranked row in the movers list: an owned item and how its price has moved over the
/// chosen window. Immutable — recomputed wholesale each time movers are refreshed.
/// </summary>
internal sealed class MoverViewModel
{
    public MoverViewModel(string name, decimal changePercent, decimal latestPrice, string currency)
    {
        Name = name;
        ChangePercent = changePercent;
        IsPositive = changePercent >= 0;
        ChangeText = $"{(IsPositive ? "+" : string.Empty)}{changePercent:N1}%";
        LatestPriceText = MoneyFormatter.Format(latestPrice, currency);
    }

    public string Name { get; }
    public decimal ChangePercent { get; }
    public bool IsPositive { get; }
    public string ChangeText { get; }
    public string LatestPriceText { get; }
}
