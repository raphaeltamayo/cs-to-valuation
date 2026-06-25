using System.Net.Http.Json;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.Infrastructure.Skinport;

/// <summary>
/// Skinport price source. One bulk call returns the entire CS2 catalogue, which we
/// project into a name → quote map for O(1) lookup during valuation.
/// </summary>
/// <remarks>
/// Skinport is strictly rate-limited (~8 requests / 5 minutes), so results are cached
/// in memory for five minutes, keyed by currency. The cache is guarded by a
/// <see cref="SemaphoreSlim"/> so a burst of concurrent callers triggers exactly one
/// network fetch (the classic double-checked lock, async-style). Time is read through
/// <see cref="TimeProvider"/> so cache expiry is deterministically testable.
/// </remarks>
public sealed class SkinportPriceService : ISkinportPriceService
{
    /// <summary>Name of the configured <see cref="HttpClient"/> (Brotli + resilience).</summary>
    public const string HttpClientName = "skinport";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SkinportPriceService(HttpClient httpClient, TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, PriceQuote>> GetPricesAsync(
        string currency, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        // Fast path: a fresh cache entry needs no lock at all.
        if (TryGetFresh(currency, now, out var cached))
        {
            return cached;
        }

        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Re-check after acquiring the lock: another caller may have just refreshed.
            if (TryGetFresh(currency, now, out cached))
            {
                return cached;
            }

            var prices = await FetchPricesAsync(currency, cancellationToken).ConfigureAwait(false);
            _cache[currency] = new CacheEntry(now + CacheDuration, prices);
            return prices;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

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
            // Skip anything we can't actually value: no name, or nothing listed.
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
                AsOfUtc = ToUtc(item.UpdatedAt),
            };
        }

        return quotes;
    }

    private bool TryGetFresh(string currency, DateTimeOffset now, out IReadOnlyDictionary<string, PriceQuote> prices)
    {
        if (_cache.TryGetValue(currency, out var entry) && entry.ExpiresAtUtc > now)
        {
            prices = entry.Prices;
            return true;
        }

        prices = default!;
        return false;
    }

    private DateTimeOffset ToUtc(long? unixSeconds) =>
        unixSeconds is { } seconds
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : _timeProvider.GetUtcNow();

    private sealed record CacheEntry(DateTimeOffset ExpiresAtUtc, IReadOnlyDictionary<string, PriceQuote> Prices);
}
