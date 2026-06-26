using CStoValuation.Infrastructure.Steam;
using CStoValuation.Tests.TestSupport;

namespace CStoValuation.Tests;

public class SteamMarketHistoryServiceTests
{
    private const string SteamCommunity = "https://steamcommunity.com/";
    private const string Item = "Falchion Case";

    [Fact]
    public async Task Parses_daily_points_when_signed_in()
    {
        var session = new SteamSession();
        session.SetCookies("steamLoginSecure=abc; sessionid=xyz");
        var service = new SteamMarketHistoryService(
            MockHttp.ClientReturning(SteamCommunity, Fixtures.Read("steam-pricehistory.json")), session);

        var points = await service.GetPriceHistoryAsync(Item, "EUR");

        Assert.Equal(3, points.Count);
        Assert.Equal(new DateTimeOffset(2019, 7, 18, 1, 0, 0, TimeSpan.Zero), points[0].DateUtc);
        Assert.Equal(1.23m, points[0].Price);
        Assert.Equal(100, points[0].Volume);
        Assert.Equal(1.30m, points[2].Price);
    }

    [Fact]
    public async Task Returns_empty_without_a_session_and_makes_no_request()
    {
        var session = new SteamSession();
        var service = new SteamMarketHistoryService(
            MockHttp.Client(SteamCommunity, _ => throw new InvalidOperationException("no request expected")),
            session);

        var points = await service.GetPriceHistoryAsync(Item, "EUR");

        Assert.Empty(points);
    }
}
