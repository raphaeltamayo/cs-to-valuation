using System.Net;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Exceptions;
using CStoValuation.Infrastructure.Steam;
using CStoValuation.Tests.TestSupport;

namespace CStoValuation.Tests;

public class SteamInventoryServiceTests
{
    private const string SteamCommunity = "https://steamcommunity.com/";
    private const string SteamId = "76561197960287930";

    [Fact]
    public async Task Maps_a_recorded_inventory_into_domain_items()
    {
        var service = new SteamInventoryService(
            MockHttp.ClientReturning(SteamCommunity, Fixtures.Read("steam-inventory.json")));

        var items = await service.GetInventoryAsync(SteamId);

        Assert.Equal(2, items.Count);

        var ak = items.Single(i => i.MarketHashName == "AK-47 | Redline (Field-Tested)");
        Assert.Equal(2, ak.Quantity);
        Assert.True(ak.Tradable);
        Assert.True(ak.Marketable);
        Assert.Equal("AK-47", ak.Weapon);
        Assert.Equal("Rifle", ak.Type);
        Assert.Equal(Rarity.Classified, ak.Rarity);
        Assert.Equal(Exterior.FieldTested, ak.Exterior);
        Assert.Equal(
            "https://community.cloudflare.steamstatic.com/economy/image/ak47redline_icon",
            ak.IconUrl);
    }

    [Fact]
    public async Task Respects_stack_amounts_and_unmapped_tags()
    {
        var service = new SteamInventoryService(
            MockHttp.ClientReturning(SteamCommunity, Fixtures.Read("steam-inventory.json")));

        var items = await service.GetInventoryAsync(SteamId);

        var sticker = items.Single(i => i.MarketHashName == "Sticker | Crown (Foil)");
        Assert.Equal(3, sticker.Quantity);
        Assert.False(sticker.Tradable);
        Assert.Equal(Rarity.Unknown, sticker.Rarity);
        Assert.Equal(Exterior.None, sticker.Exterior);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task A_forbidden_or_unauthorized_response_throws_private_inventory_exception(HttpStatusCode status)
    {
        var service = new SteamInventoryService(MockHttp.ClientWithStatus(SteamCommunity, status));

        var ex = await Assert.ThrowsAsync<PrivateInventoryException>(() => service.GetInventoryAsync(SteamId));
        Assert.Equal(SteamId, ex.SteamId64);
    }

    [Fact]
    public async Task Follows_pagination_until_more_items_is_cleared()
    {
        const string page1 = """
            {
              "success": 1, "more_items": 1, "last_assetid": "1001",
              "assets": [ { "appid": 730, "contextid": "2", "assetid": "1001", "classid": "c1", "instanceid": "0", "amount": "1" } ],
              "descriptions": [ { "classid": "c1", "instanceid": "0", "market_hash_name": "First Item", "tradable": 1, "marketable": 1 } ]
            }
            """;
        const string page2 = """
            {
              "success": 1,
              "assets": [ { "appid": 730, "contextid": "2", "assetid": "2002", "classid": "c2", "instanceid": "0", "amount": "1" } ],
              "descriptions": [ { "classid": "c2", "instanceid": "0", "market_hash_name": "Second Item", "tradable": 1, "marketable": 1 } ]
            }
            """;

        var client = MockHttp.Client(SteamCommunity, request =>
            request.RequestUri!.Query.Contains("start_assetid=1001", StringComparison.Ordinal)
                ? MockHttp.Response(page2)
                : MockHttp.Response(page1));
        var service = new SteamInventoryService(client);

        var items = await service.GetInventoryAsync(SteamId);

        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.MarketHashName == "First Item");
        Assert.Contains(items, i => i.MarketHashName == "Second Item");
    }

    [Fact]
    public async Task A_success_zero_body_throws_private_inventory_exception()
    {
        var service = new SteamInventoryService(
            MockHttp.ClientReturning(SteamCommunity, Fixtures.Read("steam-inventory-private.json")));

        await Assert.ThrowsAsync<PrivateInventoryException>(() => service.GetInventoryAsync(SteamId));
    }

    [Fact]
    public async Task An_empty_public_inventory_returns_an_empty_list()
    {
        var service = new SteamInventoryService(
            MockHttp.ClientReturning(SteamCommunity, """{ "success": 1, "total_inventory_count": 0 }"""));

        var items = await service.GetInventoryAsync(SteamId);

        Assert.Empty(items);
    }
}
