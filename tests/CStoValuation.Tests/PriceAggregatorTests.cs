using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Pricing;
using CStoValuation.Tests.TestSupport;

namespace CStoValuation.Tests;

public class PriceAggregatorTests
{
    private const string Item = "AK-47 | Redline (Field-Tested)";

    [Fact]
    public async Task Returns_the_configured_primary_providers_prices()
    {
        var skinport = new FakePriceProvider(
            PriceSource.Skinport, TestData.PriceMap(TestData.Quote(Item, 10m, source: PriceSource.Skinport)));
        var priceEmpire = new FakePriceProvider(
            PriceSource.PriceEmpire, TestData.PriceMap(TestData.Quote(Item, 12m, source: PriceSource.PriceEmpire)));
        var settings = new FakeSettingsStore(AppSettings.Default with { PrimaryPriceSource = PriceSource.PriceEmpire });
        var aggregator = new PriceAggregator([skinport, priceEmpire], settings);

        var prices = await aggregator.GetPrimaryPricesAsync("EUR");

        Assert.Equal(12m, prices[Item].Gross);
    }

    [Fact]
    public async Task Falls_back_to_the_first_provider_when_the_primary_is_not_registered()
    {
        var skinport = new FakePriceProvider(
            PriceSource.Skinport, TestData.PriceMap(TestData.Quote(Item, 10m)));
        var settings = new FakeSettingsStore(AppSettings.Default with { PrimaryPriceSource = PriceSource.CsFloat });
        var aggregator = new PriceAggregator([skinport], settings);

        var prices = await aggregator.GetPrimaryPricesAsync("EUR");

        Assert.Equal(10m, prices[Item].Gross);
    }

    [Fact]
    public async Task Collects_quotes_from_every_enabled_source_only()
    {
        var skinport = new FakePriceProvider(
            PriceSource.Skinport, TestData.PriceMap(TestData.Quote(Item, 10m, source: PriceSource.Skinport)));
        var priceEmpire = new FakePriceProvider(
            PriceSource.PriceEmpire, TestData.PriceMap(TestData.Quote(Item, 12m, source: PriceSource.PriceEmpire)));
        var disabled = new FakePriceProvider(
            PriceSource.CsFloat, TestData.PriceMap(TestData.Quote(Item, 99m, source: PriceSource.CsFloat)));
        var settings = new FakeSettingsStore(AppSettings.Default with
        {
            EnabledPriceSources = [PriceSource.Skinport, PriceSource.PriceEmpire],
        });
        var aggregator = new PriceAggregator([skinport, priceEmpire, disabled], settings);

        var quotes = await aggregator.GetAllSourceQuotesAsync(Item, "EUR");

        Assert.Equal(2, quotes.Count);
        Assert.Equal(10m, quotes[PriceSource.Skinport].Gross);
        Assert.Equal(12m, quotes[PriceSource.PriceEmpire].Gross);
        Assert.False(quotes.ContainsKey(PriceSource.CsFloat));
    }

    [Fact]
    public async Task Skips_a_source_that_has_no_quote_for_the_item()
    {
        var skinport = new FakePriceProvider(PriceSource.Skinport, TestData.PriceMap());
        var settings = new FakeSettingsStore(AppSettings.Default with { EnabledPriceSources = [PriceSource.Skinport] });
        var aggregator = new PriceAggregator([skinport], settings);

        var quotes = await aggregator.GetAllSourceQuotesAsync(Item, "EUR");

        Assert.Empty(quotes);
    }
}
