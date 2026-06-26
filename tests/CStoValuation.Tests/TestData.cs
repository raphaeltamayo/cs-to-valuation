using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.Tests;

internal static class TestData
{
    public static InventoryItem Item(string marketHashName, int quantity = 1) => new()
    {
        AssetId = "asset-" + marketHashName,
        ClassId = "class-" + marketHashName,
        InstanceId = "0",
        MarketHashName = marketHashName,
        Quantity = quantity,
        Tradable = true,
        Marketable = true,
        Rarity = Rarity.MilSpec,
        Exterior = Exterior.FieldTested,
    };

    public static PriceQuote Quote(string marketHashName, decimal gross, string currency = "EUR") => new()
    {
        MarketHashName = marketHashName,
        Source = PriceSource.Skinport,
        Currency = currency,
        Gross = gross,
        AsOfUtc = DateTimeOffset.UnixEpoch,
    };

    public static IReadOnlyDictionary<string, PriceQuote> PriceMap(params PriceQuote[] quotes) =>
        quotes.ToDictionary(q => q.MarketHashName);
}
