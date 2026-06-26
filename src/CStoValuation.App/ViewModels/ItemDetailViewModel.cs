using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CStoValuation.App.Presentation;
using CStoValuation.Core.Abstractions;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CStoValuation.App.ViewModels;

/// <summary>
/// Drives the item-detail panel: the selected item's Skinport gross/net, an on-demand Steam
/// Market second price plus trade volume (liquidity), and a LiveCharts2 line chart of its
/// recorded price history.
/// </summary>
internal sealed partial class ItemDetailViewModel : ObservableObject
{
    private const string Currency = "EUR";
    private static readonly TimeSpan HistoryWindow = TimeSpan.FromDays(30);

    private readonly ISteamMarketPriceService _steamMarketService;
    private readonly IPriceSnapshotRepository _snapshotRepository;
    private readonly TimeProvider _timeProvider;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _imageUrl;

    [ObservableProperty]
    private string _skinportGrossText = MoneyFormatter.Placeholder;

    [ObservableProperty]
    private string _skinportNetText = MoneyFormatter.Placeholder;

    [ObservableProperty]
    private string _steamPriceText = MoneyFormatter.Placeholder;

    [ObservableProperty]
    private string _steamVolumeText = MoneyFormatter.Placeholder;

    [ObservableProperty]
    private bool _isLoadingSteamPrice;

    [ObservableProperty]
    private bool _hasHistory;

    [ObservableProperty]
    private ISeries[] _series = [];

    public ItemDetailViewModel(
        ISteamMarketPriceService steamMarketService,
        IPriceSnapshotRepository snapshotRepository,
        TimeProvider? timeProvider = null)
    {
        _steamMarketService = steamMarketService;
        _snapshotRepository = snapshotRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;

        XAxes = [BuildDateAxis()];
        YAxes = [BuildPriceAxis()];
    }

    // Axes are configured once; only their data (Series) changes per selection.
    public Axis[] XAxes { get; }

    public Axis[] YAxes { get; }

    /// <summary>Loads detail for the selected row, or clears the panel when nothing is selected.</summary>
    public async Task LoadAsync(ValuedItemViewModel? item)
    {
        if (item is null)
        {
            HasSelection = false;
            return;
        }

        HasSelection = true;
        Name = item.Name;
        ImageUrl = item.ImageUrl;
        SkinportGrossText = item.UnitGrossText;
        SkinportNetText = item.UnitNetText;

        await LoadHistoryAsync(item.Name);
        await LoadSteamMarketAsync(item.Name);
    }

    private async Task LoadHistoryAsync(string marketHashName)
    {
        var since = _timeProvider.GetUtcNow() - HistoryWindow;
        var points = await _snapshotRepository.GetHistoryAsync(marketHashName, since);

        HasHistory = points.Count > 0;
        Series =
        [
            new LineSeries<DateTimePoint>
            {
                Values = points
                    .Select(point => new DateTimePoint(point.DateUtc.UtcDateTime, (double)point.Price))
                    .ToArray(),
                Name = marketHashName,
                GeometrySize = 0,
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#4B8BF5")) { StrokeThickness = 2 },
            },
        ];
    }

    private async Task LoadSteamMarketAsync(string marketHashName)
    {
        IsLoadingSteamPrice = true;
        SteamPriceText = "…";
        SteamVolumeText = "…";
        try
        {
            var quote = await _steamMarketService.GetPriceOverviewAsync(marketHashName, Currency);
            if (quote is null)
            {
                SteamPriceText = MoneyFormatter.Placeholder;
                SteamVolumeText = MoneyFormatter.Placeholder;
                return;
            }

            SteamPriceText = MoneyFormatter.Format(quote.Gross, Currency);
            SteamVolumeText = quote.Volume?.ToString("N0") ?? MoneyFormatter.Placeholder;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            SteamPriceText = "unavailable";
            SteamVolumeText = MoneyFormatter.Placeholder;
        }
        finally
        {
            IsLoadingSteamPrice = false;
        }
    }

    private static Axis BuildDateAxis() => new()
    {
        Labeler = value => new DateTime((long)value).ToString("MMM dd"),
        UnitWidth = TimeSpan.FromDays(1).Ticks,
        LabelsPaint = new SolidColorPaint(SKColor.Parse("#9AA1AD")),
        TextSize = 11,
    };

    private static Axis BuildPriceAxis() => new()
    {
        Labeler = value => value.ToString("N0"),
        LabelsPaint = new SolidColorPaint(SKColor.Parse("#9AA1AD")),
        TextSize = 11,
    };
}
