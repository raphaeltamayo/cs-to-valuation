using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface IPortfolioSnapshotRepository
{
    Task AddSnapshotAsync(PortfolioSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<PortfolioSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortfolioSnapshot>> GetHistoryAsync(
        DateTimeOffset since, CancellationToken cancellationToken = default);
}
