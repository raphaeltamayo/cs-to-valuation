using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface IValuationService
{
    InventoryValuation Value(
        IReadOnlyCollection<InventoryItem> inventory,
        IReadOnlyDictionary<string, PriceQuote> prices,
        FeeModel feeModel);
}
