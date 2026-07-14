using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CStoValuation.App.Presentation;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;

namespace CStoValuation.App.ViewModels;

internal sealed record PriceSourceOption(PriceSource Value, string Label)
{
    public override string ToString() => Label;
}

internal sealed partial class SettingsPageViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;

    [ObservableProperty]
    private PriceSourceOption _selectedPrimarySource;

    [ObservableProperty]
    private bool _isSkinportEnabled = true;

    [ObservableProperty]
    private bool _isPriceEmpireEnabled;

    [ObservableProperty]
    private bool _isCsFloatEnabled = true;

    [ObservableProperty]
    private string _priceEmpireApiKey = string.Empty;

    [ObservableProperty]
    private string _currency = "EUR";

    [ObservableProperty]
    private string? _statusMessage;

    public SettingsPageViewModel(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _selectedPrimarySource = PrimarySourceOptions[0];
    }

    public IReadOnlyList<PriceSourceOption> PrimarySourceOptions { get; } =
    [
        new(PriceSource.Skinport, PriceSource.Skinport.ToLabel()),
        new(PriceSource.PriceEmpire, PriceSource.PriceEmpire.ToLabel()),
    ];

    public IReadOnlyList<string> CurrencyOptions { get; } = ["EUR", "USD", "GBP"];

    public async Task InitializeAsync()
    {
        var settings = await _settingsStore.LoadAsync();

        SelectedPrimarySource = PrimarySourceOptions.FirstOrDefault(
            option => option.Value == settings.PrimaryPriceSource) ?? PrimarySourceOptions[0];
        IsSkinportEnabled = settings.EnabledPriceSources.Contains(PriceSource.Skinport);
        IsPriceEmpireEnabled = settings.EnabledPriceSources.Contains(PriceSource.PriceEmpire);
        IsCsFloatEnabled = settings.IsCsFloatEnabled;
        PriceEmpireApiKey = settings.PriceEmpireApiKey ?? string.Empty;
        Currency = settings.Currency;
        StatusMessage = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var enabled = new List<PriceSource>();
        if (IsSkinportEnabled)
        {
            enabled.Add(PriceSource.Skinport);
        }

        if (IsPriceEmpireEnabled)
        {
            enabled.Add(PriceSource.PriceEmpire);
        }

        if (enabled.Count == 0)
        {
            IsSkinportEnabled = true;
            enabled.Add(PriceSource.Skinport);
        }

        if (!enabled.Contains(SelectedPrimarySource.Value))
        {
            SelectedPrimarySource = PrimarySourceOptions.First(option => option.Value == enabled[0]);
        }

        var current = await _settingsStore.LoadAsync();
        var updated = current with
        {
            PrimaryPriceSource = SelectedPrimarySource.Value,
            EnabledPriceSources = enabled,
            IsCsFloatEnabled = IsCsFloatEnabled,
            PriceEmpireApiKey = string.IsNullOrWhiteSpace(PriceEmpireApiKey) ? null : PriceEmpireApiKey.Trim(),
            Currency = Currency,
        };

        await _settingsStore.SaveAsync(updated);
        StatusMessage = "Settings saved. Reconnect or refresh to apply them.";
    }
}
