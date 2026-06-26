using System.Net.Http.Json;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Caching;

namespace CStoValuation.Infrastructure.Skinport;

public sealed class SkinportSalesHistoryService : ISkinportSalesHistoryService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly TimedCache<IReadOnlyDictionary<string, ItemSalesHistory>> _cache;

    public SkinportSalesHistoryService(HttpClient httpClient, TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient;
        _cache = new TimedCache<IReadOnlyDictionary<string, ItemSalesHistory>>(
            timeProvider ?? TimeProvider.System, CacheDuration);
    }

    public Task<IReadOnlyDictionary<string, ItemSalesHistory>> GetSalesHistoryAsync(
        string currency, CancellationToken cancellationToken = default) =>
        _cache.GetOrAddAsync(currency, ct => FetchAsync(currency, ct), cancellationToken);

    private async Task<IReadOnlyDictionary<string, ItemSalesHistory>> FetchAsync(
        string currency, CancellationToken cancellationToken)
    {
        var requestUri = $"v1/sales/history?app_id=730&currency={Uri.EscapeDataString(currency)}";
        var entries = await _httpClient
            .GetFromJsonAsync<List<SkinportSalesHistoryDto>>(requestUri, cancellationToken)
            .ConfigureAwait(false) ?? [];

        var history = new Dictionary<string, ItemSalesHistory>(entries.Count, StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.MarketHashName))
            {
                continue;
            }

            history[entry.MarketHashName] = new ItemSalesHistory
            {
                MarketHashName = entry.MarketHashName,
                Currency = entry.Currency ?? currency,
                Last24Hours = ToWindow(entry.Last24Hours),
                Last7Days = ToWindow(entry.Last7Days),
                Last30Days = ToWindow(entry.Last30Days),
                Last90Days = ToWindow(entry.Last90Days),
            };
        }

        return history;
    }

    private static SalesWindow ToWindow(SkinportSalesWindowDto? dto) => dto is null
        ? SalesWindow.Empty
        : new SalesWindow
        {
            Min = dto.Min,
            Max = dto.Max,
            Average = dto.Avg,
            Median = dto.Median,
            Volume = dto.Volume,
        };
}
