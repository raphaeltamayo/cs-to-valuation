using CommunityToolkit.Mvvm.ComponentModel;
using CStoValuation.App.Presentation;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;
using CStoValuation.Core.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CStoValuation.App.ViewModels;

internal sealed partial class PerformancePageViewModel : ObservableObject
{
    private const string Currency = "EUR";
    private static readonly TimeSpan ChartWindow = TimeSpan.FromDays(90);
    private static readonly SKColor AccentColor = SKColor.Parse("#4B8BF5");
    private static readonly SKColor MutedColor = SKColor.Parse("#9AA1AD");

    private readonly InventoryPageViewModel _inventoryPage;
    private readonly IPortfolioSnapshotRepository _portfolioSnapshotRepository;
    private readonly TimeProvider _timeProvider;

    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private string _delta24HoursText = MoneyFormatter.Placeholder;
    [ObservableProperty] private bool _isDelta24HoursPositive;
    [ObservableProperty] private string _delta7DaysText = MoneyFormatter.Placeholder;
    [ObservableProperty] private bool _isDelta7DaysPositive;
    [ObservableProperty] private string _delta30DaysText = MoneyFormatter.Placeholder;
    [ObservableProperty] private bool _isDelta30DaysPositive;
    [ObservableProperty] private bool _hasChart;
    [ObservableProperty] private ISeries[] _series = [];

    public PerformancePageViewModel(
        InventoryPageViewModel inventoryPage,
        IPortfolioSnapshotRepository portfolioSnapshotRepository,
        TimeProvider? timeProvider = null)
    {
        _inventoryPage = inventoryPage;
        _portfolioSnapshotRepository = portfolioSnapshotRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;

        XAxes = [BuildDateAxis()];
        YAxes = [BuildValueAxis()];
    }

    public Axis[] XAxes { get; }

    public Axis[] YAxes { get; }

    public async Task InitializeAsync()
    {
        RefreshDeltas();
        await LoadChartAsync();
    }

    private void RefreshDeltas()
    {
        var valuation = _inventoryPage.LastValuation;
        if (valuation is null)
        {
            HasData = false;
            return;
        }

        HasData = true;
        var salesHistory = _inventoryPage.LastSalesHistory;

        SetDelta(
            PortfolioPerformance.ComputeDelta(valuation.Items, salesHistory, FeeModel.Default,
                h => h.Last24Hours.Average ?? h.Last24Hours.Median),
            value => Delta24HoursText = value, value => IsDelta24HoursPositive = value);

        SetDelta(
            PortfolioPerformance.ComputeDelta(valuation.Items, salesHistory, FeeModel.Default,
                h => h.Last7Days.Average ?? h.Last7Days.Median),
            value => Delta7DaysText = value, value => IsDelta7DaysPositive = value);

        SetDelta(
            PortfolioPerformance.ComputeDelta(valuation.Items, salesHistory, FeeModel.Default,
                h => h.Last30Days.Average ?? h.Last30Days.Median),
            value => Delta30DaysText = value, value => IsDelta30DaysPositive = value);
    }

    private void SetDelta(PerformanceDelta? delta, Action<string> setText, Action<bool> setIsPositive)
    {
        if (delta is null)
        {
            setText(MoneyFormatter.Placeholder);
            setIsPositive(true);
            return;
        }

        var sign = delta.ChangeAmount >= 0m ? "+" : "-";
        var amountText = sign + MoneyFormatter.Format(Math.Abs(delta.ChangeAmount), Currency);
        var percentSign = delta.ChangePercent >= 0m ? "+" : string.Empty;
        setText($"{amountText} ({percentSign}{delta.ChangePercent:N1}%)");
        setIsPositive(delta.ChangeAmount >= 0m);
    }

    private async Task LoadChartAsync()
    {
        var since = _timeProvider.GetUtcNow() - ChartWindow;
        var history = await _portfolioSnapshotRepository.GetHistoryAsync(since);

        HasChart = history.Count >= 2;
        Series =
        [
            new LineSeries<DateTimePoint>
            {
                Values = history
                    .Select(point => new DateTimePoint(point.TakenUtc.UtcDateTime, (double)point.TotalNet))
                    .ToArray(),
                LineSmoothness = 0.3,
                GeometrySize = history.Count <= 14 ? 4 : 0,
                Stroke = new SolidColorPaint(AccentColor) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(AccentColor.WithAlpha(36)),
                GeometryStroke = new SolidColorPaint(AccentColor) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(AccentColor),
                YToolTipLabelFormatter = point =>
                {
                    var date = new DateTime((long)point.Coordinate.SecondaryValue);
                    return $"{MoneyFormatter.Format((decimal)point.Coordinate.PrimaryValue, Currency)}  ·  {date:MMM dd, yyyy}";
                },
            },
        ];
    }

    private static Axis BuildDateAxis() => new()
    {
        Labeler = value => new DateTime((long)value).ToString("MMM dd"),
        UnitWidth = TimeSpan.FromDays(1).Ticks,
        LabelsPaint = new SolidColorPaint(MutedColor),
        TextSize = 11,
        SeparatorsPaint = null,
    };

    private static Axis BuildValueAxis() => new()
    {
        Labeler = value => MoneyFormatter.Format((decimal)value, Currency),
        LabelsPaint = new SolidColorPaint(MutedColor),
        TextSize = 11,
        SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2E36")) { StrokeThickness = 1 },
    };
}
