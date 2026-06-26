using System.Net.Http.Json;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Caching;

namespace CStoValuation.Infrastructure.Skinport;

public sealed class SkinportPriceService : ISkinportPriceService
{
    public const string HttpClientName = "skinport";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly TimedCache<IReadOnlyDictionary<string, PriceQuote>> _cache;

    public SkinportPriceService(HttpClient httpClient, TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient;
        _cache = new TimedCache<IReadOnlyDictionary<string, PriceQuote>>(
            timeProvider ?? TimeProvider.System, CacheDuration);
    }

    public Task<IReadOnlyDictionary<string, PriceQuote>> GetPricesAsync(
        string currency, CancellationToken cancellationToken = default) =>
        _cache.GetOrAddAsync(currency, ct => FetchPricesAsync(currency, ct), cancellationToken);

    private async Task<IReadOnlyDictionary<string, PriceQuote>> FetchPricesAsync(
        string currency, CancellationToken cancellationToken)
    {
        var requestUri = $"v1/items?app_id=730&currency={Uri.EscapeDataString(currency)}";
        var items = await _httpClient
            .GetFromJsonAsync<List<SkinportItemDto>>(requestUri, cancellationToken)
            .ConfigureAwait(false) ?? [];

        var quotes = new Dictionary<string, PriceQuote>(items.Count, StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.MarketHashName) || item.MinPrice is not { } minPrice)
            {
                continue;
            }

            quotes[item.MarketHashName] = new PriceQuote
            {
                MarketHashName = item.MarketHashName,
                Source = PriceSource.Skinport,
                Currency = item.Currency ?? currency,
                Gross = minPrice,
                Listings = item.Quantity,
                AsOfUtc = item.UpdatedAt is { } seconds
                    ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                    : DateTimeOffset.UnixEpoch,
            };
        }

        return quotes;
    }
}
