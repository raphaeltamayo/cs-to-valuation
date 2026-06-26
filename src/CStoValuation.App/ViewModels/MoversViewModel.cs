using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CStoValuation.Core.Abstractions;

namespace CStoValuation.App.ViewModels;

/// <summary>
/// Ranks owned items by their percentage price change over a recent window — the simplest form
/// of momentum / "biggest movers". It reads the history the background service has recorded, so
/// the list fills out over time rather than on the very first import.
/// </summary>
internal sealed partial class MoversViewModel : ObservableObject
{
    private const string Currency = "EUR";
    private const int MaxMovers = 12;
    private static readonly TimeSpan Window = TimeSpan.FromDays(7);

    private readonly IPriceSnapshotRepository _snapshotRepository;
    private readonly TimeProvider _timeProvider;

    [ObservableProperty]
    private bool _hasMovers;

    public MoversViewModel(IPriceSnapshotRepository snapshotRepository, TimeProvider? timeProvider = null)
    {
        _snapshotRepository = snapshotRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public ObservableCollection<MoverViewModel> Movers { get; } = [];

    /// <summary>Recomputes movers for the given owned items.</summary>
    public async Task LoadAsync(IEnumerable<string> ownedNames)
    {
        var since = _timeProvider.GetUtcNow() - Window;
        var computed = new List<MoverViewModel>();

        foreach (var name in ownedNames.Distinct())
        {
            var history = await _snapshotRepository.GetHistoryAsync(name, since);
            if (history.Count < 2)
            {
                continue; // need at least two points to measure a change
            }

            var first = history[0].Price;
            var last = history[^1].Price;
            if (first == 0m)
            {
                continue;
            }

            var changePercent = (last - first) / first * 100m;
            computed.Add(new MoverViewModel(name, changePercent, last, Currency));
        }

        Movers.Clear();
        foreach (var mover in computed
            .OrderByDescending(mover => Math.Abs(mover.ChangePercent))
            .Take(MaxMovers))
        {
            Movers.Add(mover);
        }

        HasMovers = Movers.Count > 0;
    }
}
