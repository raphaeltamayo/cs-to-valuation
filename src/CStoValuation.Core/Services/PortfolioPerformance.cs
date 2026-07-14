using CStoValuation.Core.Models;

namespace CStoValuation.Core.Services;

/// <summary>
/// Estimates how a portfolio's value has moved over a look-back window, without waiting for
/// the app's own recorded history to build up: each held item is re-priced at that window's
/// historical average (from <see cref="ItemSalesHistory"/>) and compared to its current line
/// value. An item with no historical data for the window is assumed unchanged, so it
/// contributes to both totals equally and never skews the delta.
/// </summary>
public static class PortfolioPerformance
{
    /// <summary>
    /// Computes the delta, or <c>null</c> when there isn't enough historical data (no priced
    /// item in <paramref name="items"/> has a usable price for the requested window).
    /// </summary>
    public static PerformanceDelta? ComputeDelta(
        IReadOnlyList<ValuedItem> items,
        IReadOnlyDictionary<string, ItemSalesHistory> salesHistory,
        FeeModel feeModel,
        Func<ItemSalesHistory, decimal?> selectHistoricalPrice)
    {
        decimal currentGross = 0m;
        decimal historicalGross = 0m;
        var matchedAny = false;

        foreach (var item in items)
        {
            if (!item.IsPriced)
            {
                continue;
            }

            currentGross += item.LineGross;

            var historicalUnitPrice = salesHistory.TryGetValue(item.Item.MarketHashName, out var history)
                ? selectHistoricalPrice(history)
                : null;

            if (historicalUnitPrice is { } unitPrice)
            {
                historicalGross += unitPrice * item.Item.Quantity;
                matchedAny = true;
            }
            else
            {
                historicalGross += item.LineGross;
            }
        }

        if (!matchedAny || historicalGross <= 0m)
        {
            return null;
        }

        var currentNet = feeModel.NetFromGross(currentGross);
        var historicalNet = feeModel.NetFromGross(historicalGross);

        return new PerformanceDelta(
            ChangeAmount: currentNet - historicalNet,
            ChangePercent: (currentGross - historicalGross) / historicalGross * 100m);
    }
}
