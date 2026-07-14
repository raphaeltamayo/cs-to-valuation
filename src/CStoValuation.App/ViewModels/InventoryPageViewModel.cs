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

internal sealed partial class InventoryPageViewModel : ObservableObject
{
    private const string Currency = "EUR";

    private readonly ISteamIdResolver _idResolver;
    private readonly ISteamInventoryService _inventoryService;
    private readonly IPriceAggregator _priceAggregator;
    private readonly IValuationService _valuationService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ISteamSignIn _steamSignIn;
    private readonly ISkinportSalesHistoryService _salesHistoryService;
    private readonly ISettingsStore _settingsStore;
    private readonly ISteamAccountLocator _accountLocator;
    private readonly ISteamProfileService _profileService;
    private readonly FeeModel _feeModel = FeeModel.Default;

    private string? _lastResolvedId;
    private AppSettings _settings = AppSettings.Default;

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

    public InventoryPageViewModel(
        ISteamIdResolver idResolver,
        ISteamInventoryService inventoryService,
        IPriceAggregator priceAggregator,
        IValuationService valuationService,
        IInventoryRepository inventoryRepository,
        ISteamSignIn steamSignIn,
        ISkinportSalesHistoryService salesHistoryService,
        ISettingsStore settingsStore,
        ISteamAccountLocator accountLocator,
        ISteamProfileService profileService,
        ItemDetailViewModel detail,
        MoversViewModel movers)
    {
        _idResolver = idResolver;
        _inventoryService = inventoryService;
        _priceAggregator = priceAggregator;
        _valuationService = valuationService;
        _inventoryRepository = inventoryRepository;
        _steamSignIn = steamSignIn;
        _salesHistoryService = salesHistoryService;
        _settingsStore = settingsStore;
        _accountLocator = accountLocator;
        _profileService = profileService;
        Detail = detail;
        Movers = movers;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = MatchesSearch;
        ItemsView.SortDescriptions.Add(
            new SortDescription(nameof(ValuedItemViewModel.LineNet), ListSortDirection.Descending));
    }

    public ObservableCollection<ValuedItemViewModel> Items { get; } = [];

    public ObservableCollection<LocalAccountViewModel> LocalAccounts { get; } = [];

    public ICollectionView ItemsView { get; }

    public ItemDetailViewModel Detail { get; }

    public MoversViewModel Movers { get; }

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnSearchTextChanged(string value) => ItemsView.Refresh();

    partial void OnSelectedItemChanged(ValuedItemViewModel? value) =>
        _ = Detail.LoadAsync(value, value is null ? null : _salesHistory.GetValueOrDefault(value.Name));

    /// <summary>
    /// The single "open this item's detail" entry point: the inventory grid drives it via
    /// <see cref="SelectedItem"/> binding directly, while the movers strip and the top-holding
    /// card (which aren't grid rows) call this command with the same underlying item so every
    /// clickable skin in the app opens the exact same detail panel.
    /// </summary>
    [RelayCommand]
    private void SelectItem(ValuedItemViewModel item) => SelectedItem = item;

    public async Task InitializeAsync()
    {
        _settings = await _settingsStore.LoadAsync();

        _ = LoadLocalAccountsAsync();

        if (!string.IsNullOrWhiteSpace(_settings.LastSteamId64))
        {
            SteamInput = _settings.LastSteamId64;
            await LoadAsync(useCachedId: false);
        }
    }

    private async Task LoadLocalAccountsAsync()
    {
        var detected = _accountLocator.GetLocalAccounts();
        LocalAccounts.Clear();

        foreach (var account in detected.Take(3))
        {
            var profile = await _profileService.GetProfileAsync(account.SteamId64);
            var displayName = profile?.PersonaName ?? account.PersonaName ?? account.SteamId64;
            LocalAccounts.Add(new LocalAccountViewModel(account.SteamId64, displayName, profile?.AvatarUrl));
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task ConnectAsLocalAccountAsync(LocalAccountViewModel account)
    {
        SteamInput = account.SteamId64;
        return LoadAsync(useCachedId: false);
    }

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

            await RememberAccountAsync(steamId);
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

    private async Task RememberAccountAsync(string steamId)
    {
        try
        {
            var profile = await _profileService.GetProfileAsync(steamId);
            _settings = _settings with
            {
                LastSteamId64 = steamId,
                PersonaName = profile?.PersonaName ?? _settings.PersonaName,
                AvatarUrl = profile?.AvatarUrl ?? _settings.AvatarUrl,
            };
            await _settingsStore.SaveAsync(_settings);
        }
        catch (Exception)
        {
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
            StatusMessage = "Fetching market prices…";
            return await _priceAggregator.GetPrimaryPricesAsync(Currency);
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
            return "Inventory loaded, but prices are unavailable " +
                   "(the pricing source may be blocked on your network) — showing items unvalued.";
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
