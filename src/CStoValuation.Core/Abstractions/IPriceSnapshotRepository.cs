using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface IPriceSnapshotRepository
{
    Task AddSnapshotsAsync(
        IEnumerable<PriceSnapshot> snapshots, CancellationToken cancellationToken = default);

    Task AddHistoryPointsAsync(
        IEnumerable<PriceHistoryPoint> points, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceHistoryPoint>> GetHistoryAsync(
        string marketHashName, DateTimeOffset since, CancellationToken cancellationToken = default);

    Task<PriceSnapshot?> GetLatestSnapshotAsync(
        string marketHashName, CancellationToken cancellationToken = default);
}
