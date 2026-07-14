using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CStoValuation.Infrastructure.Persistence;

public sealed class PortfolioSnapshotRepository : IPortfolioSnapshotRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public PortfolioSnapshotRepository(IDbContextFactory<AppDbContext> contextFactory) =>
        _contextFactory = contextFactory;

    public async Task AddSnapshotAsync(PortfolioSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.PortfolioSnapshots.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PortfolioSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.PortfolioSnapshots
            .AsNoTracking()
            .OrderByDescending(snapshot => snapshot.TakenUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PortfolioSnapshot>> GetHistoryAsync(
        DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.PortfolioSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.TakenUtc >= since)
            .OrderBy(snapshot => snapshot.TakenUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
