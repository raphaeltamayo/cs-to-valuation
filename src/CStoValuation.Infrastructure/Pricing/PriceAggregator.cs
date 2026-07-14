using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.Infrastructure.Pricing;

public sealed class PriceAggregator : IPriceAggregator
{
    private readonly IReadOnlyList<IPriceProvider> _providers;
    private readonly ISettingsStore _settingsStore;

    public PriceAggregator(IEnumerable<IPriceProvider> providers, ISettingsStore settingsStore)
    {
        _providers = providers.ToList();
        _settingsStore = settingsStore;
    }

    public async Task<IReadOnlyDictionary<string, PriceQuote>> GetPrimaryPricesAsync(
        string currency, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var primary = ResolvePrimary(settings.PrimaryPriceSource);
        return await primary.GetAllPricesAsync(currency, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<PriceSource, PriceQuote>> GetAllSourceQuotesAsync(
        string marketHashName, string currency, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var enabled = _providers.Where(provider => settings.EnabledPriceSources.Contains(provider.Source));

        var result = new Dictionary<PriceSource, PriceQuote>();
        foreach (var provider in enabled)
        {
            var prices = await provider.GetAllPricesAsync(currency, cancellationToken).ConfigureAwait(false);
            if (prices.TryGetValue(marketHashName, out var quote))
            {
                result[provider.Source] = quote;
            }
        }

        return result;
    }

    private IPriceProvider ResolvePrimary(PriceSource primarySource) =>
        _providers.FirstOrDefault(provider => provider.Source == primarySource) ?? _providers.First();
}
