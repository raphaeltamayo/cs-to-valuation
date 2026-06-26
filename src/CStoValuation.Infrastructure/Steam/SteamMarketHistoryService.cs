using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CStoValuation.Infrastructure.Steam;

public sealed class SteamMarketHistoryService : ISteamMarketHistoryService
{
    private readonly HttpClient _httpClient;
    private readonly ISteamSession _session;
    private readonly ILogger<SteamMarketHistoryService> _logger;

    public SteamMarketHistoryService(
        HttpClient httpClient, ISteamSession session, ILogger<SteamMarketHistoryService>? logger = null)
    {
        _httpClient = httpClient;
        _session = session;
        _logger = logger ?? NullLogger<SteamMarketHistoryService>.Instance;
    }

    public async Task<IReadOnlyList<PriceHistoryPoint>> GetPriceHistoryAsync(
        string marketHashName, string currency, CancellationToken cancellationToken = default)
    {
        var cookie = _session.CookieHeader;
        if (string.IsNullOrEmpty(cookie))
        {
            return [];
        }

        var requestUri =
            $"market/pricehistory/?appid=730&currency={ToSteamCurrencyId(currency)}" +
            $"&market_hash_name={Uri.EscapeDataString(marketHashName)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("Cookie", cookie);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Price history HTTP {Status} for {Item}", (int)response.StatusCode, marketHashName);
            return [];
        }

        var payload = await response.Content
            .ReadFromJsonAsync<PriceHistoryResponse>(cancellationToken)
            .ConfigureAwait(false);

        if (payload is not { Success: true, Prices: not null })
        {
            return [];
        }

        var points = new List<PriceHistoryPoint>(payload.Prices.Count);
        foreach (var entry in payload.Prices)
        {
            if (entry.Length < 2 || ParseDate(entry[0].GetString()) is not { } date)
            {
                continue;
            }

            points.Add(new PriceHistoryPoint
            {
                MarketHashName = marketHashName,
                DateUtc = date,
                Price = entry[1].GetDecimal(),
                Volume = ParseVolume(entry.Length > 2 ? entry[2].GetString() : null),
            });
        }

        _logger.LogInformation("Price history: {Count} points for {Item}", points.Count, marketHashName);
        return points;
    }

    private static DateTimeOffset? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return null;
        }

        var normalized = $"{parts[0]} {parts[1]} {parts[2]} {parts[3].TrimEnd(':')}";
        return DateTime.TryParseExact(
            normalized, "MMM d yyyy H", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? new DateTimeOffset(parsed, TimeSpan.Zero)
            : null;
    }

    private static int? ParseVolume(string? raw) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static int ToSteamCurrencyId(string currency) => currency switch
    {
        "USD" => 1,
        "GBP" => 2,
        "EUR" => 3,
        _ => 3,
    };

    private sealed record PriceHistoryResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("prices")]
        public List<JsonElement[]>? Prices { get; init; }
    }
}
