using CStoValuation.Core.Enums;
using CStoValuation.Infrastructure.Skinport;
using CStoValuation.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace CStoValuation.Tests;

public class SkinportPriceServiceTests
{
    private const string Skinport = "https://api.skinport.com/";

    [Fact]
    public async Task Parses_recorded_prices_into_quotes_keyed_by_name()
    {
        var service = new SkinportPriceService(
            MockHttp.ClientReturning(Skinport, Fixtures.Read("skinport-items.json")));

        var prices = await service.GetPricesAsync("EUR");

        Assert.Equal(2, prices.Count);

        var ak = prices["AK-47 | Redline (Field-Tested)"];
        Assert.Equal(12.34m, ak.Gross);
        Assert.Equal(42, ak.Listings);
        Assert.Equal("EUR", ak.Currency);
        Assert.Equal(PriceSource.Skinport, ak.Source);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000), ak.AsOfUtc);

        Assert.False(prices.ContainsKey("Currently Unlisted Item"));
    }

    [Fact]
    public async Task Caches_results_for_five_minutes_then_refetches()
    {
        var callCount = 0;
        var client = MockHttp.Client(Skinport, _ =>
        {
            callCount++;
            return MockHttp.Response(Fixtures.Read("skinport-items.json"));
        });

        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var service = new SkinportPriceService(client, clock);

        await service.GetPricesAsync("EUR");
        await service.GetPricesAsync("EUR");
        Assert.Equal(1, callCount);

        clock.Advance(TimeSpan.FromMinutes(4));
        await service.GetPricesAsync("EUR");
        Assert.Equal(1, callCount);

        clock.Advance(TimeSpan.FromMinutes(2));
        await service.GetPricesAsync("EUR");
        Assert.Equal(2, callCount);
    }
}
