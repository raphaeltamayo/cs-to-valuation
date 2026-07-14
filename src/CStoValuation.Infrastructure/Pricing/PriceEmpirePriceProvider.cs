using System.Net.Http.Json;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Caching;

namespace CStoValuation.Infrastructure.Pricing;

/// <remarks>
/// PriceEmpire aggregates many marketplaces per item; we take the cheapest of the reported
/// prices as the gross quote, mirroring how Skinport's own "min_price" is defined. The exact
/// currency/precision returned by the live API was not verified against a real API key while
/// building this, so this mapping should be re-checked against a real response once a key is
/// available.
/// </remarks>
public sealed class PriceEmpirePriceProvider : IPriceProvider
{
    public const string HttpClientName = "priceempire";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly ISettingsStore _settingsStore;
    private readonly TimedCache<IReadOnlyDictionary<string, PriceQuote>> _cache;

    public PriceEmpirePriceProvider(HttpClient httpClient, ISettingsStore settingsStore, TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient;
        _settingsStore = settingsStore;
        _cache = new TimedCache<IReadOnlyDictionary<string, PriceQuote>>(
            timeProvider ?? TimeProvider.System, CacheDuration);
    }

    public PriceSource Source => PriceSource.PriceEmpire;

    public Task<IReadOnlyDictionary<string, PriceQuote>> GetAllPricesAsync(
        string currency, CancellationToken cancellationToken = default) =>
        _cache.GetOrAddAsync(currency, ct => FetchPricesAsync(currency, ct), cancellationToken);

    private async Task<IReadOnlyDictionary<string, PriceQuote>> FetchPricesAsync(
        string currency, CancellationToken cancellationToken)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(settings.PriceEmpireApiKey))
        {
            return new Dictionary<string, PriceQuote>();
        }

        var requestUri = $"v3/items/prices?app_id=730&currency={Uri.EscapeDataString(currency)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("X-API-Key", settings.PriceEmpireApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new Dictionary<string, PriceQuote>();
        }

        var items = await response.Content
            .ReadFromJsonAsync<List<PriceEmpireItemDto>>(cancellationToken)
            .ConfigureAwait(false) ?? [];

        var quotes = new Dictionary<string, PriceQuote>(items.Count, StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.MarketHashName) || item.Prices is not { Count: > 0 })
            {
                continue;
            }

            var cheapest = item.Prices
                .Where(price => price.Price is > 0m)
                .OrderBy(price => price.Price)
                .FirstOrDefault();

            if (cheapest is null)
            {
                continue;
            }

            quotes[item.MarketHashName] = new PriceQuote
            {
                MarketHashName = item.MarketHashName,
                Source = PriceSource.PriceEmpire,
                Currency = currency,
                Gross = cheapest.Price!.Value,
                Listings = cheapest.Count,
                AsOfUtc = cheapest.UpdatedAt ?? DateTimeOffset.UtcNow,
            };
        }

        return quotes;
    }
}
