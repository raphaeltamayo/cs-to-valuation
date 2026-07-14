using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Persistence;
using CStoValuation.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace CStoValuation.Tests;

public class RepositoryTests : IDisposable
{
    private readonly SqliteInMemoryContextFactory _factory = new();

    [Fact]
    public async Task Inventory_round_trips_through_the_cache()
    {
        var repository = new InventoryRepository(_factory, new FakeTimeProvider());
        var items = new[]
        {
            new InventoryItem
            {
                AssetId = "1", ClassId = "c1", InstanceId = "0",
                MarketHashName = "AK-47 | Redline (Field-Tested)",
                Quantity = 2, Tradable = true, Marketable = true,
                Rarity = Rarity.Classified, Exterior = Exterior.FieldTested,
            },
        };

        await repository.SaveInventoryAsync("steam-1", items);
        var loaded = await repository.GetCachedInventoryAsync("steam-1");

        var item = Assert.Single(loaded);
        Assert.Equal("AK-47 | Redline (Field-Tested)", item.MarketHashName);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(Rarity.Classified, item.Rarity);
    }

    [Fact]
    public async Task Saving_inventory_replaces_the_previous_cache_for_that_account()
    {
        var repository = new InventoryRepository(_factory, new FakeTimeProvider());

        await repository.SaveInventoryAsync("steam-1", [Item("First Item")]);
        await repository.SaveInventoryAsync("steam-1", [Item("Second Item"), Item("Third Item")]);

        var loaded = await repository.GetCachedInventoryAsync("steam-1");

        Assert.Equal(2, loaded.Count);
        Assert.DoesNotContain(loaded, i => i.MarketHashName == "First Item");
    }

    [Fact]
    public async Task Inventory_is_isolated_per_account()
    {
        var repository = new InventoryRepository(_factory, new FakeTimeProvider());

        await repository.SaveInventoryAsync("steam-1", [Item("Mine")]);
        await repository.SaveInventoryAsync("steam-2", [Item("Theirs")]);

        var mine = await repository.GetCachedInventoryAsync("steam-1");

        Assert.Equal("Mine", Assert.Single(mine).MarketHashName);
    }

    [Fact]
    public async Task History_is_returned_in_time_order_and_filtered_by_since()
    {
        var repository = new PriceSnapshotRepository(_factory);
        var name = "AK-47 | Redline (Field-Tested)";
        await repository.AddHistoryPointsAsync(
        [
            new PriceHistoryPoint { MarketHashName = name, DateUtc = Day(3), Price = 13m },
            new PriceHistoryPoint { MarketHashName = name, DateUtc = Day(1), Price = 11m },
            new PriceHistoryPoint { MarketHashName = name, DateUtc = Day(2), Price = 12m },
            new PriceHistoryPoint { MarketHashName = "Other", DateUtc = Day(2), Price = 99m },
        ]);

        var history = await repository.GetHistoryAsync(name, Day(2));

        Assert.Equal(2, history.Count);
        Assert.Equal(12m, history[0].Price);
        Assert.Equal(13m, history[1].Price);
    }

    [Fact]
    public async Task Latest_snapshot_returns_the_most_recent_for_the_item()
    {
        var repository = new PriceSnapshotRepository(_factory);
        var name = "AK-47 | Redline (Field-Tested)";
        await repository.AddSnapshotsAsync(
        [
            new PriceSnapshot { MarketHashName = name, Min = 10m, TakenUtc = Day(1) },
            new PriceSnapshot { MarketHashName = name, Min = 12m, TakenUtc = Day(3) },
            new PriceSnapshot { MarketHashName = name, Min = 11m, TakenUtc = Day(2) },
        ]);

        var latest = await repository.GetLatestSnapshotAsync(name);

        Assert.NotNull(latest);
        Assert.Equal(12m, latest!.Min);
    }

    [Fact]
    public async Task Portfolio_history_is_returned_in_time_order_and_filtered_by_since()
    {
        var repository = new PortfolioSnapshotRepository(_factory);
        await repository.AddSnapshotAsync(new PortfolioSnapshot { TotalNet = 300m, TakenUtc = Day(3) });
        await repository.AddSnapshotAsync(new PortfolioSnapshot { TotalNet = 100m, TakenUtc = Day(1) });
        await repository.AddSnapshotAsync(new PortfolioSnapshot { TotalNet = 200m, TakenUtc = Day(2) });

        var history = await repository.GetHistoryAsync(Day(2));

        Assert.Equal(2, history.Count);
        Assert.Equal(200m, history[0].TotalNet);
        Assert.Equal(300m, history[1].TotalNet);
    }

    [Fact]
    public async Task Portfolio_latest_snapshot_returns_the_most_recent_one()
    {
        var repository = new PortfolioSnapshotRepository(_factory);
        await repository.AddSnapshotAsync(new PortfolioSnapshot { TotalNet = 100m, TakenUtc = Day(1) });
        await repository.AddSnapshotAsync(new PortfolioSnapshot { TotalNet = 300m, TakenUtc = Day(3) });
        await repository.AddSnapshotAsync(new PortfolioSnapshot { TotalNet = 200m, TakenUtc = Day(2) });

        var latest = await repository.GetLatestSnapshotAsync();

        Assert.NotNull(latest);
        Assert.Equal(300m, latest!.TotalNet);
    }

    [Fact]
    public async Task Portfolio_latest_snapshot_is_null_when_none_exist()
    {
        var repository = new PortfolioSnapshotRepository(_factory);

        var latest = await repository.GetLatestSnapshotAsync();

        Assert.Null(latest);
    }

    private static InventoryItem Item(string name) => new()
    {
        AssetId = "a", ClassId = "c", InstanceId = "0", MarketHashName = name, Quantity = 1,
    };

    private static DateTimeOffset Day(int day) => new(2026, 1, day, 0, 0, 0, TimeSpan.Zero);

    public void Dispose() => _factory.Dispose();
}
