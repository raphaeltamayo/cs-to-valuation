using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CStoValuation.App.Presentation;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CStoValuation.App.ViewModels;

internal sealed record HistoryRange(int Days, string Label)
{
    public override string ToString() => Label;
}

internal sealed partial class ItemDetailViewModel : ObservableObject
{
    private const string Currency = "EUR";
    private static readonly SKColor AccentColor = SKColor.Parse("#4B8BF5");
    private static readonly SKColor MutedColor = SKColor.Parse("#9AA1AD");

    private readonly ISteamMarketHistoryService _historyService;
    private readonly ISteamMarketPriceService _steamMarketService;
    private readonly IPriceAggregator _priceAggregator;
    private readonly ICsFloatPriceService _csFloatPriceService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ISettingsStore _settingsStore;
    private readonly TimeProvider _timeProvider;

    private IReadOnlyList<PriceHistoryPoint> _fullHistory = [];
    private ItemSalesHistory? _salesHistory;
    private bool _hasRealHistory;

    [ObservableProperty] private bool _hasSelection;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _imageUrl;
    [ObservableProperty] private string _skinportGrossText = MoneyFormatter.Placeholder;
    [ObservableProperty] private string _skinportNetText = MoneyFormatter.Placeholder;
    [ObservableProperty] private string _steamPriceText = MoneyFormatter.Placeholder;
    [ObservableProperty] private string _steamVolumeText = MoneyFormatter.Placeholder;
    [ObservableProperty] private bool _isLoadingSteamPrice;
    [ObservableProperty] private bool _isPriceEmpireEnabled;
    [ObservableProperty] private string _priceEmpireGrossText = MoneyFormatter.Placeholder;
    [ObservableProperty] private bool _isCsFloatEnabled;
    [ObservableProperty] private string _csFloatPriceText = MoneyFormatter.Placeholder;
    [ObservableProperty] private bool _hasHistory;
    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private HistoryRange _selectedRange;

    public ItemDetailViewModel(
        ISteamMarketHistoryService historyService,
        ISteamMarketPriceService steamMarketService,
        IPriceAggregator priceAggregator,
        ICsFloatPriceService csFloatPriceService,
        IExchangeRateService exchangeRateService,
        ISettingsStore settingsStore,
        TimeProvider? timeProvider = null)
    {
        _historyService = historyService;
        _steamMarketService = steamMarketService;
        _priceAggregator = priceAggregator;
        _csFloatPriceService = csFloatPriceService;
        _exchangeRateService = exchangeRateService;
        _settingsStore = settingsStore;
        _timeProvider = timeProvider ?? TimeProvider.System;

        _selectedRange = Ranges[1];
        XAxes = [BuildDateAxis()];
        YAxes = [BuildPriceAxis()];
    }

    public IReadOnlyList<HistoryRange> Ranges { get; } =
    [
        new(7, "7D"),
        new(30, "30D"),
        new(90, "90D"),
        new(365, "1Y"),
    ];

    public Axis[] XAxes { get; }

    public Axis[] YAxes { get; }

    public async Task LoadAsync(ValuedItemViewModel? item, ItemSalesHistory? salesHistory)
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
        _salesHistory = salesHistory;

        await LoadSupplementalDataAsync(item.Name);
    }

    /// <summary>
    /// Loads detail for a catalog entry that isn't in the owner's inventory: there is no
    /// valuation line to read gross/net from, so the primary-source price is passed in
    /// directly (already known from the catalog's own bulk price join) and net is derived
    /// with the same fee model used everywhere else.
    /// </summary>
    public async Task LoadForCatalogAsync(string marketHashName, string? imageUrl, decimal? gross, string currency)
    {
        HasSelection = true;
        Name = marketHashName;
        ImageUrl = imageUrl;
        SkinportGrossText = MoneyFormatter.Format(gross, currency);
        SkinportNetText = gross is { } value
            ? MoneyFormatter.Format(FeeModel.Default.NetFromGross(value), currency)
            : MoneyFormatter.Placeholder;
        _salesHistory = null;

        await LoadSupplementalDataAsync(marketHashName);
    }

    private async Task LoadSupplementalDataAsync(string marketHashName)
    {
        await LoadHistoryAsync(marketHashName);
        await LoadSteamMarketAsync(marketHashName);
        await LoadOtherSourcesAsync(marketHashName);
    }

    partial void OnSelectedRangeChanged(HistoryRange value) => RebuildSeries();

    private async Task LoadHistoryAsync(string marketHashName)
    {
        try
        {
            _fullHistory = await _historyService.GetPriceHistoryAsync(marketHashName, Currency);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _fullHistory = [];
        }

        _hasRealHistory = _fullHistory.Count >= 2;
        RebuildSeries();
    }

    private void RebuildSeries()
    {
        var points = _hasRealHistory ? PointsForWindow() : BuildTrendPoints(_salesHistory);
        HasHistory = points.Length >= 2;

        Series =
        [
            new LineSeries<DateTimePoint>
            {
                Values = points,
                LineSmoothness = 0.3,
                GeometrySize = points.Length <= 8 ? 5 : 0,
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

    private DateTimePoint[] PointsForWindow()
    {
        var since = _timeProvider.GetUtcNow() - TimeSpan.FromDays(SelectedRange.Days);
        var windowed = _fullHistory.Where(point => point.DateUtc >= since).ToList();

        if (windowed.Count < 2)
        {
            windowed = [.. _fullHistory];
        }

        return windowed
            .Select(point => new DateTimePoint(point.DateUtc.UtcDateTime, (double)point.Price))
            .ToArray();
    }

    private DateTimePoint[] BuildTrendPoints(ItemSalesHistory? salesHistory)
    {
        if (salesHistory is null)
        {
            return [];
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var candidates = new (DateTime At, decimal? Value)[]
        {
            (now.AddDays(-90), salesHistory.Last90Days.Average ?? salesHistory.Last90Days.Median),
            (now.AddDays(-30), salesHistory.Last30Days.Average ?? salesHistory.Last30Days.Median),
            (now.AddDays(-7), salesHistory.Last7Days.Average ?? salesHistory.Last7Days.Median),
            (now, salesHistory.Last24Hours.Average ?? salesHistory.Last24Hours.Median),
        };

        return candidates
            .Where(point => point.Value is not null)
            .Select(point => new DateTimePoint(point.At, (double)point.Value!.Value))
            .ToArray();
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

    private async Task LoadOtherSourcesAsync(string marketHashName)
    {
        var settings = await _settingsStore.LoadAsync();
        IsPriceEmpireEnabled = settings.EnabledPriceSources.Contains(PriceSource.PriceEmpire);
        IsCsFloatEnabled = settings.IsCsFloatEnabled;

        PriceEmpireGrossText = MoneyFormatter.Placeholder;
        if (IsPriceEmpireEnabled)
        {
            try
            {
                var quotes = await _priceAggregator.GetAllSourceQuotesAsync(marketHashName, Currency);
                PriceEmpireGrossText = quotes.TryGetValue(PriceSource.PriceEmpire, out var quote)
                    ? MoneyFormatter.Format(quote.Gross, Currency)
                    : MoneyFormatter.Placeholder;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                PriceEmpireGrossText = "unavailable";
            }
        }

        CsFloatPriceText = MoneyFormatter.Placeholder;
        if (IsCsFloatEnabled)
        {
            try
            {
                var csFloatQuote = await _csFloatPriceService.GetPriceOverviewAsync(marketHashName);
                if (csFloatQuote is not null)
                {
                    var converted = await _exchangeRateService.ConvertAsync(
                        csFloatQuote.Gross, csFloatQuote.Currency, Currency);
                    CsFloatPriceText = MoneyFormatter.Format(converted, Currency);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                CsFloatPriceText = "unavailable";
            }
        }
    }

    private static Axis BuildDateAxis() => new()
    {
        Labeler = value => new DateTime((long)value).ToString("MMM dd"),
        UnitWidth = TimeSpan.FromDays(1).Ticks,
        LabelsPaint = new SolidColorPaint(MutedColor),
        TextSize = 11,
        SeparatorsPaint = null,
    };

    private static Axis BuildPriceAxis() => new()
    {
        Labeler = value => MoneyFormatter.Format((decimal)value, Currency),
        LabelsPaint = new SolidColorPaint(MutedColor),
        TextSize = 11,
        SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2E36")) { StrokeThickness = 1 },
    };
}
