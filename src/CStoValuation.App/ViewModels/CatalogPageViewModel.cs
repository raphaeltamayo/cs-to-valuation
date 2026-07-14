using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;

namespace CStoValuation.App.ViewModels;

internal sealed partial class CatalogPageViewModel : ObservableObject
{
    private const string Currency = "EUR";
    private const int ColumnsPerRow = 5;

    private readonly ICatalogService _catalogService;
    private readonly IPriceAggregator _priceAggregator;

    private IReadOnlyList<CatalogItemViewModel> _allItems = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    public CatalogPageViewModel(ICatalogService catalogService, IPriceAggregator priceAggregator, ItemDetailViewModel detail)
    {
        _catalogService = catalogService;
        _priceAggregator = priceAggregator;
        Detail = detail;
    }

    public ObservableCollection<CatalogRowViewModel> Rows { get; } = [];

    public ItemDetailViewModel Detail { get; }

    partial void OnSearchTextChanged(string value) => RebuildRows();

    public async Task InitializeAsync()
    {
        if (_allItems.Count > 0)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = "Loading catalog…";
        try
        {
            var entries = await _catalogService.GetCatalogAsync();
            var prices = await FetchPricesAsync();

            _allItems = entries
                .Select(entry => new CatalogItemViewModel(
                    entry.MarketHashName,
                    entry.ImageUrl,
                    prices.TryGetValue(entry.MarketHashName, out var quote) ? quote.Gross : null,
                    Currency))
                .ToList();

            RebuildRows();
            StatusMessage = _allItems.Count == 0
                ? "Couldn't load the catalog. Please try again later."
                : $"{_allItems.Count} skins loaded.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task SelectItemAsync(CatalogItemViewModel item) =>
        Detail.LoadForCatalogAsync(item.Name, item.ImageUrl, item.Gross, item.Currency);

    private async Task<IReadOnlyDictionary<string, PriceQuote>> FetchPricesAsync()
    {
        try
        {
            return await _priceAggregator.GetPrimaryPricesAsync(Currency);
        }
        catch (Exception)
        {
            return new Dictionary<string, PriceQuote>();
        }
    }

    private void RebuildRows()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allItems
            : _allItems.Where(item => item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Rows.Clear();
        for (var i = 0; i < filtered.Count; i += ColumnsPerRow)
        {
            Rows.Add(new CatalogRowViewModel(filtered.Skip(i).Take(ColumnsPerRow).ToList()));
        }
    }
}
