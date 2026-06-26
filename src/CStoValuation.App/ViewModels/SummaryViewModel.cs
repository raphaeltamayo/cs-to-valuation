using CStoValuation.App.Presentation;
using CStoValuation.Core.Models;

namespace CStoValuation.App.ViewModels;

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
