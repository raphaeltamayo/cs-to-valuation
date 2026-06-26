using CStoValuation.Core.Models;
using CStoValuation.Core.Services;
using Xunit;

namespace CStoValuation.Tests;

public class ValuationServiceTests
{
    private readonly ValuationService _sut = new();

    [Fact]
    public void Empty_inventory_produces_a_zero_valuation()
    {
        var result = _sut.Value([], TestData.PriceMap(), FeeModel.Default);

        Assert.Empty(result.Items);
        Assert.Equal(0m, result.TotalGross);
        Assert.Equal(0m, result.TotalNet);
        Assert.Equal(0, result.PricedCount);
        Assert.Equal(0, result.UnpricedCount);
    }

    [Fact]
    public void A_priced_item_contributes_gross_and_net_of_fees()
    {
        var inventory = new[] { TestData.Item("AK-47 | Redline (Field-Tested)") };
        var prices = TestData.PriceMap(TestData.Quote("AK-47 | Redline (Field-Tested)", 25.00m));

        var result = _sut.Value(inventory, prices, FeeModel.Default);

        var line = Assert.Single(result.Items);
        Assert.True(line.IsPriced);
        Assert.Equal(25.00m, line.LineGross);
        Assert.Equal(23.00m, line.LineNet);
        Assert.Equal(25.00m, result.TotalGross);
        Assert.Equal(23.00m, result.TotalNet);
        Assert.Equal(1, result.PricedCount);
        Assert.Equal(0, result.UnpricedCount);
    }

    [Fact]
    public void Quantity_multiplies_the_line_value()
    {
        var inventory = new[] { TestData.Item("Glock-18 | Water Elemental (Minimal Wear)", quantity: 3) };
        var prices = TestData.PriceMap(TestData.Quote("Glock-18 | Water Elemental (Minimal Wear)", 10.00m));

        var result = _sut.Value(inventory, prices, FeeModel.Default);

        var line = Assert.Single(result.Items);
        Assert.Equal(30.00m, line.LineGross);
        Assert.Equal(27.60m, line.LineNet);
    }

    [Fact]
    public void An_item_with_no_matching_price_is_counted_as_unpriced()
    {
        var inventory = new[] { TestData.Item("Some Unpriced Sticker") };

        var result = _sut.Value(inventory, TestData.PriceMap(), FeeModel.Default);

        var line = Assert.Single(result.Items);
        Assert.False(line.IsPriced);
        Assert.Null(line.Quote);
        Assert.Equal(0m, line.LineGross);
        Assert.Equal(0m, line.LineNet);
        Assert.Equal(0, result.PricedCount);
        Assert.Equal(1, result.UnpricedCount);
    }

    [Fact]
    public void A_mixed_inventory_totals_only_the_priced_lines()
    {
        var inventory = new[]
        {
            TestData.Item("AK-47 | Redline (Field-Tested)"),
            TestData.Item("Dropped Knife", quantity: 1),
            TestData.Item("AWP | Asiimov (Field-Tested)", quantity: 2),
        };
        var prices = TestData.PriceMap(
            TestData.Quote("AK-47 | Redline (Field-Tested)", 25.00m),
            TestData.Quote("AWP | Asiimov (Field-Tested)", 50.00m));

        var result = _sut.Value(inventory, prices, FeeModel.Default);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(2, result.PricedCount);
        Assert.Equal(1, result.UnpricedCount);
        Assert.Equal(125.00m, result.TotalGross);
        Assert.Equal(115.00m, result.TotalNet);
    }

    [Fact]
    public void Currency_is_taken_from_the_supplied_quotes()
    {
        var inventory = new[] { TestData.Item("AK-47 | Redline (Field-Tested)") };
        var prices = TestData.PriceMap(
            TestData.Quote("AK-47 | Redline (Field-Tested)", 25.00m, currency: "USD"));

        var result = _sut.Value(inventory, prices, FeeModel.Default);

        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Currency_falls_back_to_eur_when_no_prices_are_available()
    {
        var inventory = new[] { TestData.Item("AK-47 | Redline (Field-Tested)") };

        var result = _sut.Value(inventory, TestData.PriceMap(), FeeModel.Default);

        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    public void Null_arguments_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => _sut.Value(null!, TestData.PriceMap(), FeeModel.Default));
        Assert.Throws<ArgumentNullException>(
            () => _sut.Value([], null!, FeeModel.Default));
        Assert.Throws<ArgumentNullException>(
            () => _sut.Value([], TestData.PriceMap(), null!));
    }
}
