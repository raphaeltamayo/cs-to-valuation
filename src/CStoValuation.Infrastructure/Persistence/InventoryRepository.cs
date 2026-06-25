using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CStoValuation.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed inventory cache. Uses <see cref="IDbContextFactory{TContext}"/> to spin
/// up a short-lived context per operation — the right pattern for a desktop app where a
/// background service and the UI may touch the database concurrently and a single shared,
/// scoped context would not be thread-safe.
/// </summary>
public sealed class InventoryRepository : IInventoryRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;

    public InventoryRepository(IDbContextFactory<AppDbContext> contextFactory, TimeProvider? timeProvider = null)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task SaveInventoryAsync(
        string steamId64, IReadOnlyCollection<InventoryItem> items, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Replace the previous cache for this account wholesale — a fresh import is the
        // new source of truth. ExecuteDelete issues a single DELETE without loading rows.
        await context.InventoryItems
            .Where(item => item.SteamId64 == steamId64)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = _timeProvider.GetUtcNow();
        var rows = items.Select(item => ToEntity(steamId64, item, now));
        await context.InventoryItems.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryItem>> GetCachedInventoryAsync(
        string steamId64, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // AsNoTracking: we only read, so skip the change-tracker overhead.
        var rows = await context.InventoryItems
            .AsNoTracking()
            .Where(item => item.SteamId64 == steamId64)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(ToDomain).ToList();
    }

    private static CachedInventoryItem ToEntity(string steamId64, InventoryItem item, DateTimeOffset cachedAtUtc) => new()
    {
        SteamId64 = steamId64,
        AssetId = item.AssetId,
        ClassId = item.ClassId,
        InstanceId = item.InstanceId,
        MarketHashName = item.MarketHashName,
        Quantity = item.Quantity,
        Tradable = item.Tradable,
        Marketable = item.Marketable,
        IconUrl = item.IconUrl,
        Weapon = item.Weapon,
        Rarity = item.Rarity,
        Exterior = item.Exterior,
        Type = item.Type,
        CachedAtUtc = cachedAtUtc,
    };

    private static InventoryItem ToDomain(CachedInventoryItem row) => new()
    {
        AssetId = row.AssetId,
        ClassId = row.ClassId,
        InstanceId = row.InstanceId,
        MarketHashName = row.MarketHashName,
        Quantity = row.Quantity,
        Tradable = row.Tradable,
        Marketable = row.Marketable,
        IconUrl = row.IconUrl,
        Weapon = row.Weapon,
        Rarity = row.Rarity,
        Exterior = row.Exterior,
        Type = row.Type,
    };
}
