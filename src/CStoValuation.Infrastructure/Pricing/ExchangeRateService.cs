using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CStoValuation.Core.Abstractions;
using CStoValuation.Infrastructure.Caching;

namespace CStoValuation.Infrastructure.Pricing;

public sealed class ExchangeRateService : IExchangeRateService
{
    public const string HttpClientName = "exchangerate";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly HttpClient _httpClient;
    private readonly TimedCache<decimal> _cache;

    public ExchangeRateService(HttpClient httpClient, TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient;
        _cache = new TimedCache<decimal>(timeProvider ?? TimeProvider.System, CacheDuration);
    }

    public async Task<decimal> ConvertAsync(
        decimal amount, string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        var cacheKey = $"{fromCurrency}_{toCurrency}".ToUpperInvariant();
        var rate = await _cache.GetOrAddAsync(
            cacheKey, ct => FetchRateAsync(fromCurrency, toCurrency, ct), cancellationToken).ConfigureAwait(false);

        return decimal.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<decimal> FetchRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        var requestUri = $"latest?from={Uri.EscapeDataString(fromCurrency)}&to={Uri.EscapeDataString(toCurrency)}";
        var response = await _httpClient
            .GetFromJsonAsync<FrankfurterResponse>(requestUri, cancellationToken)
            .ConfigureAwait(false);

        if (response?.Rates is null || !response.Rates.TryGetValue(toCurrency, out var rate))
        {
            throw new InvalidOperationException($"No exchange rate from {fromCurrency} to {toCurrency} was returned.");
        }

        return rate;
    }

    private sealed record FrankfurterResponse
    {
        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; init; }
    }
}
