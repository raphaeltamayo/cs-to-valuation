using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CStoValuation.App.Presentation;
using CStoValuation.Core.Models;

namespace CStoValuation.App.ViewModels;

internal enum MoverMetric
{
    Percentage,

    UnitValue,

    LineValue,
}

internal sealed record MetricOption(MoverMetric Value, string Label)
{
    public override string ToString() => Label;
}

internal sealed partial class MoversViewModel : ObservableObject
{
    private const string Currency = "EUR";
    private const int MaxMovers = 12;

    private readonly List<MoverData> _all = [];

    [ObservableProperty]
    private bool _hasMovers;

    [ObservableProperty]
    private MoverMetric _selectedMetric = MoverMetric.Percentage;

    public ObservableCollection<MoverViewModel> Movers { get; } = [];

    public IReadOnlyList<MetricOption> Metrics { get; } =
    [
        new(MoverMetric.Percentage, "% change"),
        new(MoverMetric.UnitValue, "Per unit (€)"),
        new(MoverMetric.LineValue, "Total (€)"),
    ];

    public void Load(IReadOnlyDictionary<string, ItemSalesHistory> salesHistory, IEnumerable<ValuedItemViewModel> ownedItems)
    {
        _all.Clear();

        foreach (var item in ownedItems)
        {
            if (!salesHistory.TryGetValue(item.Name, out var history))
            {
                continue;
            }

            var recent = history.Last7Days.Average;
            var baseline = history.Last30Days.Average;
            if (recent is not { } recentAvg || baseline is not { } baselineAvg || baselineAvg <= 0m)
            {
                continue;
            }

            var unitChange = recentAvg - baselineAvg;
            _all.Add(new MoverData(
                Name: item.Name,
                Percent: unitChange / baselineAvg * 100m,
                UnitChange: unitChange,
                LineChange: unitChange * item.Quantity,
                Latest: history.Last24Hours.Average ?? recentAvg));
        }

        Rebuild();
    }

    partial void OnSelectedMetricChanged(MoverMetric value) => Rebuild();

    private void Rebuild()
    {
        Movers.Clear();
        foreach (var mover in _all
            .OrderByDescending(mover => Math.Abs(SortKey(mover)))
            .Take(MaxMovers))
        {
            Movers.Add(new MoverViewModel(
                mover.Name,
                DisplayText(mover),
                SortKey(mover) >= 0m,
                MoneyFormatter.Format(mover.Latest, Currency)));
        }

        HasMovers = Movers.Count > 0;
    }

    private decimal SortKey(MoverData mover) => SelectedMetric switch
    {
        MoverMetric.UnitValue => mover.UnitChange,
        MoverMetric.LineValue => mover.LineChange,
        _ => mover.Percent,
    };

    private string DisplayText(MoverData mover) => SelectedMetric switch
    {
        MoverMetric.UnitValue => FormatSigned(mover.UnitChange),
        MoverMetric.LineValue => FormatSigned(mover.LineChange),
        _ => $"{(mover.Percent >= 0m ? "+" : string.Empty)}{mover.Percent.ToString("N1", CultureInfo.CurrentCulture)}%",
    };

    private static string FormatSigned(decimal value)
    {
        var sign = value >= 0m ? "+" : "-";
        return sign + MoneyFormatter.Format(Math.Abs(value), Currency);
    }

    private sealed record MoverData(string Name, decimal Percent, decimal UnitChange, decimal LineChange, decimal Latest);
}
