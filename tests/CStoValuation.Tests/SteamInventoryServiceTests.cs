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

        // Two distinct items; the asset with no matching description is dropped.
        Assert.Equal(2, items.Count);

        var ak = items.Single(i => i.MarketHashName == "AK-47 | Redline (Field-Tested)");
        Assert.Equal(2, ak.Quantity);                 // two assets of the same class were stacked
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
        Assert.Equal(3, sticker.Quantity);            // single asset with amount "3"
        Assert.False(sticker.Tradable);
        Assert.Equal(Rarity.Unknown, sticker.Rarity); // no rarity tag in the fixture
        Assert.Equal(Exterior.None, sticker.Exterior);
    }

    [Fact]
    public async Task A_403_response_throws_private_inventory_exception()
    {
        var service = new SteamInventoryService(
            MockHttp.ClientWithStatus(SteamCommunity, HttpStatusCode.Forbidden));

        var ex = await Assert.ThrowsAsync<PrivateInventoryException>(() => service.GetInventoryAsync(SteamId));
        Assert.Equal(SteamId, ex.SteamId64);
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
