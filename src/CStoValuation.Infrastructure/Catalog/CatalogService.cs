using System.Net.Http.Json;
using System.Text.Json;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;

namespace CStoValuation.Infrastructure.Catalog;

/// <remarks>
/// The full CS2 skin catalog (every skin, not just owned ones) rarely changes, so it is
/// downloaded once from the community-maintained ByMykel/CSGO-API dataset and cached to disk;
/// on later launches the cached copy is reused until it is more than a day old. This keeps
/// the catalog available offline and avoids re-downloading a multi-megabyte file every run.
/// </remarks>
public sealed class CatalogService : ICatalogService
{
    public const string HttpClientName = "catalog";
    private const string RequestUri = "ByMykel/CSGO-API/main/public/api/en/skins_not_grouped.json";

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);

    private readonly HttpClient _httpClient;
    private readonly string _cacheFilePath;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyList<CatalogEntry>? _loaded;

    public CatalogService(HttpClient httpClient, string cacheFilePath, TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient;
        _cacheFilePath = cacheFilePath;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<CatalogEntry>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded is not null)
        {
            return _loaded;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_loaded is not null)
            {
                return _loaded;
            }

            _loaded = await LoadAsync(cancellationToken).ConfigureAwait(false);
            return _loaded;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<CatalogEntry>> LoadAsync(CancellationToken cancellationToken)
    {
        var cache = await TryReadCacheAsync(cancellationToken).ConfigureAwait(false);
        if (cache is not null && _timeProvider.GetUtcNow() - cache.CachedAtUtc <= RefreshInterval)
        {
            return cache.Entries;
        }

        var fetched = await FetchFromRemoteAsync(cancellationToken).ConfigureAwait(false);
        if (fetched.Count > 0)
        {
            await WriteCacheAsync(fetched, cancellationToken).ConfigureAwait(false);
            return fetched;
        }

        return cache?.Entries ?? [];
    }

    private async Task<CatalogCacheDto?> TryReadCacheAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_cacheFilePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_cacheFilePath);
            return await JsonSerializer.DeserializeAsync<CatalogCacheDto>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    private async Task WriteCacheAsync(IReadOnlyList<CatalogEntry> entries, CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var cache = new CatalogCacheDto(_timeProvider.GetUtcNow(), entries.ToList());
            await using var stream = File.Create(_cacheFilePath);
            await JsonSerializer.SerializeAsync(stream, cache, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
        }
    }

    private sealed record CatalogCacheDto(DateTimeOffset CachedAtUtc, List<CatalogEntry> Entries);

    private async Task<List<CatalogEntry>> FetchFromRemoteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var skins = await _httpClient
                .GetFromJsonAsync<List<CatalogSkinDto>>(RequestUri, cancellationToken)
                .ConfigureAwait(false) ?? [];

            return skins
                .Where(skin => !string.IsNullOrEmpty(skin.MarketHashName))
                .Select(skin => new CatalogEntry(skin.MarketHashName!, skin.Image))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return [];
        }
    }
}
