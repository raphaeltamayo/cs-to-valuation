using CStoValuation.Core.Models;
using CStoValuation.Core.Services;
using Xunit;

namespace CStoValuation.Tests;

public class PortfolioPerformanceTests
{
    private static readonly FeeModel Fee = FeeModel.Default;

    [Fact]
    public void Returns_null_when_there_is_no_historical_data_for_any_item()
    {
        var items = new[] { Priced("AK-47 | Redline (Field-Tested)", gross: 100m) };
        var history = new Dictionary<string, ItemSalesHistory>();

        var delta = PortfolioPerformance.ComputeDelta(items, history, Fee, h => h.Last7Days.Average);

        Assert.Null(delta);
    }

    [Fact]
    public void Computes_a_positive_delta_when_the_current_price_is_higher_than_the_historical_one()
    {
        var items = new[] { Priced("AK-47 | Redline (Field-Tested)", gross: 110m) };
        var history = new Dictionary<string, ItemSalesHistory>
        {
            ["AK-47 | Redline (Field-Tested)"] = History(last7DaysAverage: 100m),
        };

        var delta = PortfolioPerformance.ComputeDelta(items, history, Fee, h => h.Last7Days.Average);

        Assert.NotNull(delta);
        Assert.True(delta!.ChangeAmount > 0m);
        Assert.Equal(10m, delta.ChangePercent, 3);
    }

    [Fact]
    public void Computes_a_negative_delta_when_the_current_price_is_lower_than_the_historical_one()
    {
        var items = new[] { Priced("AWP | Asiimov (Field-Tested)", gross: 90m) };
        var history = new Dictionary<string, ItemSalesHistory>
        {
            ["AWP | Asiimov (Field-Tested)"] = History(last7DaysAverage: 100m),
        };

        var delta = PortfolioPerformance.ComputeDelta(items, history, Fee, h => h.Last7Days.Average);

        Assert.NotNull(delta);
        Assert.True(delta!.ChangeAmount < 0m);
        Assert.Equal(-10m, delta.ChangePercent, 3);
    }

    [Fact]
    public void Multiplies_the_historical_price_by_quantity()
    {
        var items = new[] { Priced("Glock-18 | Fade (Factory New)", gross: 30m, quantity: 3) };
        var history = new Dictionary<string, ItemSalesHistory>
        {
            ["Glock-18 | Fade (Factory New)"] = History(last7DaysAverage: 10m),
        };

        var delta = PortfolioPerformance.ComputeDelta(items, history, Fee, h => h.Last7Days.Average);

        Assert.NotNull(delta);
        Assert.Equal(0m, delta!.ChangePercent, 3);
    }

    [Fact]
    public void Ignores_unpriced_lines()
    {
        var items = new[]
        {
            Priced("AK-47 | Redline (Field-Tested)", gross: 110m),
            Unpriced("Some Sticker"),
        };
        var history = new Dictionary<string, ItemSalesHistory>
        {
            ["AK-47 | Redline (Field-Tested)"] = History(last7DaysAverage: 100m),
        };

        var delta = PortfolioPerformance.ComputeDelta(items, history, Fee, h => h.Last7Days.Average);

        Assert.NotNull(delta);
        Assert.Equal(10m, delta!.ChangePercent, 3);
    }

    [Fact]
    public void Treats_an_item_with_no_historical_price_as_unchanged()
    {
        var items = new[]
        {
            Priced("AK-47 | Redline (Field-Tested)", gross: 110m),
            Priced("No History Item", gross: 50m),
        };
        var history = new Dictionary<string, ItemSalesHistory>
        {
            ["AK-47 | Redline (Field-Tested)"] = History(last7DaysAverage: 100m),
        };

        var delta = PortfolioPerformance.ComputeDelta(items, history, Fee, h => h.Last7Days.Average);

        // current = 110 + 50 = 160, historical = 100 + 50 (unchanged) = 150
        Assert.NotNull(delta);
        Assert.Equal((160m - 150m) / 150m * 100m, delta!.ChangePercent, 3);
    }

    private static ValuedItem Priced(string name, decimal gross, int quantity = 1) => new()
    {
        Item = TestData.Item(name, quantity),
        Quote = TestData.Quote(name, gross / quantity),
        LineGross = gross,
        LineNet = FeeModel.Default.NetFromGross(gross),
    };

    private static ValuedItem Unpriced(string name) => new() { Item = TestData.Item(name) };

    private static ItemSalesHistory History(decimal last7DaysAverage) => new()
    {
        MarketHashName = "irrelevant",
        Currency = "EUR",
        Last7Days = new SalesWindow { Average = last7DaysAverage },
    };
}
