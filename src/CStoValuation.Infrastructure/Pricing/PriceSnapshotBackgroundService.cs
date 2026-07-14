using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CStoValuation.Infrastructure.Pricing;

public sealed class PriceSnapshotBackgroundService : BackgroundService
{
    private const string Currency = "EUR";
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IPriceAggregator _priceAggregator;
    private readonly IPriceSnapshotRepository _snapshotRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PriceSnapshotBackgroundService> _logger;

    public PriceSnapshotBackgroundService(
        IDbContextFactory<AppDbContext> contextFactory,
        IPriceAggregator priceAggregator,
        IPriceSnapshotRepository snapshotRepository,
        ILogger<PriceSnapshotBackgroundService> logger,
        TimeProvider? timeProvider = null)
    {
        _contextFactory = contextFactory;
        _priceAggregator = priceAggregator;
        _snapshotRepository = snapshotRepository;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, _timeProvider, stoppingToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                await SnapshotOwnedItemsAsync(stoppingToken).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SnapshotOwnedItemsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ownedNames = await GetOwnedNamesAsync(cancellationToken).ConfigureAwait(false);
            if (ownedNames.Count == 0)
            {
                return;
            }

            var prices = await _priceAggregator.GetPrimaryPricesAsync(Currency, cancellationToken).ConfigureAwait(false);
            var takenUtc = _timeProvider.GetUtcNow();

            var snapshots = new List<PriceSnapshot>();
            var historyPoints = new List<PriceHistoryPoint>();
            foreach (var name in ownedNames)
            {
                if (!prices.TryGetValue(name, out var quote))
                {
                    continue;
                }

                snapshots.Add(new PriceSnapshot
                {
                    MarketHashName = name,
                    Source = quote.Source,
                    Min = quote.Gross,
                    Listings = quote.Listings,
                    Currency = quote.Currency,
                    TakenUtc = takenUtc,
                });

                historyPoints.Add(new PriceHistoryPoint
                {
                    MarketHashName = name,
                    DateUtc = takenUtc,
                    Price = quote.Gross,
                });
            }

            if (snapshots.Count == 0)
            {
                return;
            }

            await _snapshotRepository.AddSnapshotsAsync(snapshots, cancellationToken).ConfigureAwait(false);
            await _snapshotRepository.AddHistoryPointsAsync(historyPoints, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Recorded {Count} price snapshots.", snapshots.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Price snapshot tick failed; will retry on the next interval.");
        }
    }

    private async Task<IReadOnlyList<string>> GetOwnedNamesAsync(CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.InventoryItems
            .AsNoTracking()
            .Select(item => item.MarketHashName)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
