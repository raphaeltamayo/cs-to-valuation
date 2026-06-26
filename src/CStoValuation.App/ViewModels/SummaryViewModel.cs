using CStoValuation.App.Presentation;
using CStoValuation.Core.Models;

namespace CStoValuation.App.ViewModels;

/// <summary>
/// The headline numbers shown in the summary panel. Immutable: when a new valuation is
/// produced the <see cref="MainViewModel"/> swaps in a whole new instance rather than
/// mutating fields, so the UI updates from a single property change.
/// </summary>
internal sealed class SummaryViewModel
{
    private SummaryViewModel(InventoryValuation valuation)
    {
        Currency = valuation.Currency;
        TotalGrossText = MoneyFormatter.Format(valuation.TotalGross, valuation.Currency);
        TotalNetText = MoneyFormatter.Format(valuation.TotalNet, valuation.Currency);
        ItemCount = valuation.Items.Count;
        PricedCount = valuation.PricedCount;
        UnpricedCount = valuation.UnpricedCount;
    }

    public string Currency { get; }
    public string TotalGrossText { get; }
    public string TotalNetText { get; }
    public int ItemCount { get; }
    public int PricedCount { get; }
    public int UnpricedCount { get; }

    public string CountsText => $"{ItemCount} items · {PricedCount} priced · {UnpricedCount} unpriced";

    public static SummaryViewModel FromValuation(InventoryValuation valuation) => new(valuation);

    public static SummaryViewModel Empty(string currency) => new(InventoryValuation.Empty(currency));
}
