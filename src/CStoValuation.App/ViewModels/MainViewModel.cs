using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Exceptions;
using CStoValuation.Core.Models;

namespace CStoValuation.App.ViewModels;

/// <summary>
/// The shell view-model: it owns the connect/refresh workflow and the inventory it produces.
/// </summary>
/// <remarks>
/// CommunityToolkit.Mvvm's source generators turn the <c>[ObservableProperty]</c> fields into
/// full <c>INotifyPropertyChanged</c> properties and the <c>[RelayCommand]</c> methods into
/// <c>ICommand</c>s at compile time — no hand-written boilerplate, no reflection. Every awaited
/// call here deliberately keeps the UI synchronization context (no <c>ConfigureAwait(false)</c>),
/// so continuations resume on the UI thread and it is safe to touch the observable collection.
/// </remarks>
internal sealed partial class MainViewModel : ObservableObject
{
    private const string Currency = "EUR";

    private readonly ISteamIdResolver _idResolver;
    private readonly ISteamInventoryService _inventoryService;
    private readonly ISkinportPriceService _priceService;
    private readonly IValuationService _valuationService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly FeeModel _feeModel = FeeModel.Default;

    private string? _lastResolvedId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _steamInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
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
        IInventoryRepository inventoryRepository)
    {
        _idResolver = idResolver;
        _inventoryService = inventoryService;
        _priceService = priceService;
        _valuationService = valuationService;
        _inventoryRepository = inventoryRepository;

        // A view over the same collection gives us live sort + filter without copying data.
        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = MatchesSearch;
        ItemsView.SortDescriptions.Add(
            new SortDescription(nameof(ValuedItemViewModel.LineNet), ListSortDirection.Descending));
    }

    public ObservableCollection<ValuedItemViewModel> Items { get; } = [];

    public ICollectionView ItemsView { get; }

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // Re-applying the filter is as simple as asking the view to refresh.
    partial void OnSearchTextChanged(string value) => ItemsView.Refresh();

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private Task ConnectAsync() => LoadAsync(useCachedId: false);

    private bool CanConnect() => !IsBusy && !string.IsNullOrWhiteSpace(SteamInput);

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

        try
        {
            var steamId = useCachedId && _lastResolvedId is not null
                ? _lastResolvedId
                : await ResolveAccountAsync();

            var inventory = await ImportInventoryAsync(steamId);
            var prices = await FetchPricesAsync();

            var valuation = _valuationService.Value(inventory, prices, _feeModel);
            ShowValuation(valuation);
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
            ErrorMessage = "Couldn't reach Steam or Skinport. Check your connection and try again.";
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
            // Network trouble: fall back to the last good import if we have one.
            var cached = await _inventoryRepository.GetCachedInventoryAsync(steamId);
            if (cached.Count == 0)
            {
                throw;
            }

            StatusMessage = "Offline — showing your last cached inventory.";
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
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Without prices we still show the inventory, just unvalued.
            StatusMessage = "Prices are unavailable right now — showing items without valuation.";
            return new Dictionary<string, PriceQuote>();
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

        if (StatusMessage is null || !StatusMessage.StartsWith("Offline", StringComparison.Ordinal))
        {
            StatusMessage = valuation.Items.Count == 0
                ? "This inventory is empty."
                : $"Valued {valuation.PricedCount} of {valuation.Items.Count} items.";
        }
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
