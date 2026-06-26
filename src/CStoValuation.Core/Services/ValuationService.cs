using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;

namespace CStoValuation.Core.Services;

public sealed class ValuationService : IValuationService
{
    private const string FallbackCurrency = "EUR";

    public InventoryValuation Value(
        IReadOnlyCollection<InventoryItem> inventory,
        IReadOnlyDictionary<string, PriceQuote> prices,
        FeeModel feeModel)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(prices);
        ArgumentNullException.ThrowIfNull(feeModel);

        var currency = ResolveCurrency(prices);

        var valuedItems = new List<ValuedItem>(inventory.Count);
        decimal totalGross = 0m;
        decimal totalNet = 0m;
        var pricedCount = 0;
        var unpricedCount = 0;

        foreach (var item in inventory)
        {
            if (!prices.TryGetValue(item.MarketHashName, out var quote))
            {
                unpricedCount++;
                valuedItems.Add(new ValuedItem { Item = item, Quote = null });
                continue;
            }

            var lineGross = quote.Gross * item.Quantity;
            var lineNet = feeModel.NetFromGross(lineGross);

            totalGross += lineGross;
            totalNet += lineNet;
            pricedCount++;

            valuedItems.Add(new ValuedItem
            {
                Item = item,
                Quote = quote,
                LineGross = lineGross,
                LineNet = lineNet,
            });
        }

        return new InventoryValuation
        {
            Items = valuedItems,
            TotalGross = decimal.Round(totalGross, 2, MidpointRounding.AwayFromZero),
            TotalNet = decimal.Round(totalNet, 2, MidpointRounding.AwayFromZero),
            Currency = currency,
            PricedCount = pricedCount,
            UnpricedCount = unpricedCount,
        };
    }

    private static string ResolveCurrency(IReadOnlyDictionary<string, PriceQuote> prices)
    {
        foreach (var quote in prices.Values)
        {
            return quote.Currency;
        }

        return FallbackCurrency;
    }
}
