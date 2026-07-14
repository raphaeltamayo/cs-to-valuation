using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Pricing;
using CStoValuation.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace CStoValuation.Tests;

public class PriceEmpirePriceProviderTests
{
    private const string PriceEmpire = "https://api.pricempire.com/";

    [Fact]
    public async Task Returns_the_cheapest_reported_price_per_item()
    {
        var settings = new FakeSettingsStore(AppSettings.Default with { PriceEmpireApiKey = "test-key" });
        var provider = new PriceEmpirePriceProvider(
            MockHttp.ClientReturning(PriceEmpire, Fixtures.Read("priceempire-items.json")), settings);

        var prices = await provider.GetAllPricesAsync("EUR");

        var ak = prices["AK-47 | Redline (Field-Tested)"];
        Assert.Equal(12.34m, ak.Gross);
        Assert.Equal(8, ak.Listings);
        Assert.Equal(PriceSource.PriceEmpire, ak.Source);
        Assert.False(prices.ContainsKey("No Prices Item"));
    }

    [Fact]
    public async Task Returns_no_prices_when_no_api_key_is_configured()
    {
        var settings = new FakeSettingsStore(AppSettings.Default with { PriceEmpireApiKey = null });
        var provider = new PriceEmpirePriceProvider(
            MockHttp.Client(PriceEmpire, _ => throw new InvalidOperationException("no request expected")), settings);

        var prices = await provider.GetAllPricesAsync("EUR");

        Assert.Empty(prices);
    }

    [Fact]
    public async Task Sends_the_configured_api_key_as_a_header()
    {
        HttpRequestMessage? captured = null;
        var settings = new FakeSettingsStore(AppSettings.Default with { PriceEmpireApiKey = "my-secret-key" });
        var provider = new PriceEmpirePriceProvider(
            MockHttp.Client(PriceEmpire, request =>
            {
                captured = request;
                return MockHttp.Response(Fixtures.Read("priceempire-items.json"));
            }),
            settings);

        await provider.GetAllPricesAsync("EUR");

        Assert.Equal("my-secret-key", captured!.Headers.GetValues("X-API-Key").Single());
    }

    [Fact]
    public async Task Caches_results_for_five_minutes()
    {
        var callCount = 0;
        var client = MockHttp.Client(PriceEmpire, _ =>
        {
            callCount++;
            return MockHttp.Response(Fixtures.Read("priceempire-items.json"));
        });
        var settings = new FakeSettingsStore(AppSettings.Default with { PriceEmpireApiKey = "test-key" });
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var provider = new PriceEmpirePriceProvider(client, settings, clock);

        await provider.GetAllPricesAsync("EUR");
        await provider.GetAllPricesAsync("EUR");
        Assert.Equal(1, callCount);

        clock.Advance(TimeSpan.FromMinutes(6));
        await provider.GetAllPricesAsync("EUR");
        Assert.Equal(2, callCount);
    }
}
