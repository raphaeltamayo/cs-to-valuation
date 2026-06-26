using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CStoValuation.App.Authentication;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Exceptions;
using CStoValuation.Core.Models;

namespace CStoValuation.App.ViewModels;

internal sealed partial class MainViewModel : ObservableObject
{
    private const string Currency = "EUR";

    private readonly ISteamIdResolver _idResolver;
    private readonly ISteamInventoryService _inventoryService;
    private readonly ISkinportPriceService _priceService;
    private readonly IValuationService _valuationService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ISteamSignIn _steamSignIn;
    private readonly ISkinportSalesHistoryService _salesHistoryService;
    private readonly FeeModel _feeModel = FeeModel.Default;

    private string? _lastResolvedId;

    private IReadOnlyDictionary<string, ItemSalesHistory> _salesHistory =
        new Dictionary<string, ItemSalesHistory>();

    private bool _loadedFromCache;
    private bool _pricesUnavailable;

    [ObservableProperty]
    private ValuedItemViewModel? _selectedItem;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _steamInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(SignInWithSteamCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isPrivateInventoryError;

    [ObservableProperty]
    private SummaryViewModel _summary = SummaryViewModel.Empty(Currency);

    [ObservableProperty]
    private string _searchText = string.Empty;

    public MainViewModel(
        ISteamIdResolver idResolver,
        ISteamInventoryService inventoryService,
        ISkinportPriceService priceService,
        IValuationService valuationService,
        IInventoryRepository inventoryRepository,
        ISteamSignIn steamSignIn,
        ISkinportSalesHistoryService salesHistoryService,
        ItemDetailViewModel detail,
        MoversViewModel movers)
    {
        _idResolver = idResolver;
        _inventoryService = inventoryService;
        _priceService = priceService;
        _valuationService = valuationService;
        _inventoryRepository = inventoryRepository;
        _steamSignIn = steamSignIn;
        _salesHistoryService = salesHistoryService;
        Detail = detail;
        Movers = movers;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = MatchesSearch;
        ItemsView.SortDescriptions.Add(
            new SortDescription(nameof(ValuedItemViewModel.LineNet), ListSortDirection.Descending));
    }

    public ObservableCollection<ValuedItemViewModel> Items { get; } = [];

    public ICollectionView ItemsView { get; }

    public ItemDetailViewModel Detail { get; }

    public MoversViewModel Movers { get; }

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnSearchTextChanged(string value) => ItemsView.Refresh();

    partial void OnSelectedItemChanged(ValuedItemViewModel? value) =>
        _ = Detail.LoadAsync(value, value is null ? null : _salesHistory.GetValueOrDefault(value.Name));

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private Task ConnectAsync() => LoadAsync(useCachedId: false);

    private bool CanConnect() => !IsBusy && !string.IsNullOrWhiteSpace(SteamInput);

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task SignInWithSteamAsync()
    {
        var steamId = await _steamSignIn.SignInAsync();
        if (steamId is null)
        {
            return;
        }

        SteamInput = steamId;
        await LoadAsync(useCachedId: false);
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private Task RefreshAsync() => LoadAsync(useCachedId: true);

    private bool CanRefresh() => !IsBusy && _lastResolvedId is not null;

    private async Task LoadAsync(bool useCachedId)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        IsPrivateInventoryError = false;
        _loadedFromCache = false;
        _pricesUnavailable = false;

        try
        {
            var steamId = useCachedId && _lastResolvedId is not null
                ? _lastResolvedId
                : await ResolveAccountAsync();

            var inventory = await ImportInventoryAsync(steamId);
            var prices = await FetchPricesAsync();

            var valuation = _valuationService.Value(inventory, prices, _feeModel);
            ShowValuation(valuation);

            _salesHistory = await FetchSalesHistoryAsync();
            Movers.Load(_salesHistory, Items);
        }
        catch (PrivateInventoryException)
        {
            ShowPrivateInventoryError();
        }
        catch (FormatException)
        {
            ErrorMessage =
                "That doesn't look like a Steam ID or profile URL. Paste a SteamID64, a " +
                "profiles/… URL, or your custom id/… URL.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage =
                "Couldn't reach Steam. It may be down, rate-limiting requests, or blocked on " +
                "your network. Please try again in a moment.";
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong while loading your inventory. Please try again.";
        }
        finally
        {
            IsBusy = false;
            RefreshCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task<string> ResolveAccountAsync()
    {
        StatusMessage = "Resolving Steam account…";
        var steamId = await _idResolver.ResolveAsync(SteamInput);
        _lastResolvedId = steamId;
        return steamId;
    }

    private async Task<IReadOnlyList<InventoryItem>> ImportInventoryAsync(string steamId)
    {
        try
        {
            StatusMessage = "Importing inventory from Steam…";
            var inventory = await _inventoryService.GetInventoryAsync(steamId);
            await _inventoryRepository.SaveInventoryAsync(steamId, inventory);
            return inventory;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            var cached = await _inventoryRepository.GetCachedInventoryAsync(steamId);
            if (cached.Count == 0)
            {
                throw;
            }

            _loadedFromCache = true;
            return cached;
        }
    }

    private async Task<IReadOnlyDictionary<string, PriceQuote>> FetchPricesAsync()
    {
        try
        {
            StatusMessage = "Fetching market prices from Skinport…";
            return await _priceService.GetPricesAsync(Currency);
        }
        catch (Exception)
        {
            _pricesUnavailable = true;
            return new Dictionary<string, PriceQuote>();
        }
    }

    private async Task<IReadOnlyDictionary<string, ItemSalesHistory>> FetchSalesHistoryAsync()
    {
        try
        {
            return await _salesHistoryService.GetSalesHistoryAsync(Currency);
        }
        catch (Exception)
        {
            return new Dictionary<string, ItemSalesHistory>();
        }
    }

    private void ShowValuation(InventoryValuation valuation)
    {
        Items.Clear();
        foreach (var item in valuation.Items)
        {
            Items.Add(new ValuedItemViewModel(item, valuation.Currency));
        }

        ItemsView.Refresh();
        Summary = SummaryViewModel.FromValuation(valuation);
        StatusMessage = BuildStatusMessage(valuation);
    }

    private string BuildStatusMessage(InventoryValuation valuation)
    {
        if (valuation.Items.Count == 0)
        {
            return "This inventory is empty.";
        }

        if (_pricesUnavailable)
        {
            return "Inventory loaded, but Skinport prices are unavailable " +
                   "(it may be blocked on your network) — showing items unvalued.";
        }

        var summary = $"Valued {valuation.PricedCount} of {valuation.Items.Count} items.";
        return _loadedFromCache ? $"Offline — showing your last cached inventory. {summary}" : summary;
    }

    private void ShowPrivateInventoryError()
    {
        IsPrivateInventoryError = true;
        ErrorMessage =
            "This inventory is private. In Steam, open Profile → Edit Profile → Privacy Settings " +
            "and set \"Game details\" (and inventory) to Public, then try again.";
        StatusMessage = null;
    }

    private bool MatchesSearch(object candidate)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return candidate is ValuedItemViewModel item
            && item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }
}
