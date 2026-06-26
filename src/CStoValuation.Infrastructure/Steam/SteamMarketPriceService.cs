using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.Infrastructure.Steam;

public sealed class SteamMarketPriceService : ISteamMarketPriceService
{
    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;

    public SteamMarketPriceService(HttpClient httpClient, TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<PriceQuote?> GetPriceOverviewAsync(
        string marketHashName, string currency, CancellationToken cancellationToken = default)
    {
        var requestUri =
            $"market/priceoverview/?appid=730&currency={ToSteamCurrencyId(currency)}" +
            $"&market_hash_name={Uri.EscapeDataString(marketHashName)}";

        var overview = await _httpClient
            .GetFromJsonAsync<PriceOverviewDto>(requestUri, cancellationToken)
            .ConfigureAwait(false);

        if (overview is not { Success: true })
        {
            return null;
        }

        var gross = SteamPriceParser.ParsePrice(overview.LowestPrice ?? overview.MedianPrice);
        if (gross is null)
        {
            return null;
        }

        return new PriceQuote
        {
            MarketHashName = marketHashName,
            Source = PriceSource.SteamMarket,
            Currency = currency,
            Gross = gross.Value,
            Volume = SteamPriceParser.ParseVolume(overview.Volume),
            AsOfUtc = _timeProvider.GetUtcNow(),
        };
    }

    private static int ToSteamCurrencyId(string currency) => currency switch
    {
        "USD" => 1,
        "GBP" => 2,
        "EUR" => 3,
        _ => 3,
    };

    private sealed record PriceOverviewDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("lowest_price")]
        public string? LowestPrice { get; init; }

        [JsonPropertyName("median_price")]
        public string? MedianPrice { get; init; }

        [JsonPropertyName("volume")]
        public string? Volume { get; init; }
    }
}
