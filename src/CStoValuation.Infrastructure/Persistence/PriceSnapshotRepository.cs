using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CStoValuation.Infrastructure.Persistence;

public sealed class PriceSnapshotRepository : IPriceSnapshotRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public PriceSnapshotRepository(IDbContextFactory<AppDbContext> contextFactory) =>
        _contextFactory = contextFactory;

    public async Task AddSnapshotsAsync(
        IEnumerable<PriceSnapshot> snapshots, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.PriceSnapshots.AddRangeAsync(snapshots, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddHistoryPointsAsync(
        IEnumerable<PriceHistoryPoint> points, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.PriceHistoryPoints.AddRangeAsync(points, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PriceHistoryPoint>> GetHistoryAsync(
        string marketHashName, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.PriceHistoryPoints
            .AsNoTracking()
            .Where(point => point.MarketHashName == marketHashName && point.DateUtc >= since)
            .OrderBy(point => point.DateUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PriceSnapshot?> GetLatestSnapshotAsync(
        string marketHashName, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.PriceSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.MarketHashName == marketHashName)
            .OrderByDescending(snapshot => snapshot.TakenUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
