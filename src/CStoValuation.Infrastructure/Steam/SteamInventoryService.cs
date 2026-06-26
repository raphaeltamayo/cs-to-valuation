using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Exceptions;
using CStoValuation.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CStoValuation.Infrastructure.Steam;

public sealed class SteamInventoryService : ISteamInventoryService
{
    private const string IconCdnBase = "https://community.cloudflare.steamstatic.com/economy/image/";

    private readonly HttpClient _httpClient;
    private readonly ILogger<SteamInventoryService> _logger;

    public SteamInventoryService(HttpClient httpClient, ILogger<SteamInventoryService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<SteamInventoryService>.Instance;
    }

    private const int PageSize = 2000;
    private const int MaxPages = 25;

    public async Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(
        string steamId64, CancellationToken cancellationToken = default)
    {
        var assets = new List<SteamAssetDto>();
        var descriptions = new List<SteamDescriptionDto>();

        string? startAssetId = null;
        for (var page = 0; page < MaxPages; page++)
        {
            var payload = await FetchPageAsync(steamId64, startAssetId, cancellationToken).ConfigureAwait(false);

            if (payload.Assets is null || payload.Descriptions is null)
            {
                break;
            }

            assets.AddRange(payload.Assets);
            descriptions.AddRange(payload.Descriptions);

            if (payload.MoreItems != 1 || string.IsNullOrEmpty(payload.LastAssetId))
            {
                break;
            }

            startAssetId = payload.LastAssetId;
        }

        var items = JoinAndMap(assets, descriptions);
        _logger.LogInformation("Mapped {Count} inventory stacks from {Assets} assets", items.Count, assets.Count);
        return items;
    }

    private async Task<SteamInventoryResponse> FetchPageAsync(
        string steamId64, string? startAssetId, CancellationToken cancellationToken)
    {
        var requestUri = $"inventory/{steamId64}/730/2?l=english&count={PageSize}";
        if (startAssetId is not null)
        {
            requestUri += $"&start_assetid={startAssetId}";
        }

        _logger.LogInformation("Requesting inventory page: {RequestUri}", requestUri);
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Inventory response {StatusCode} for {SteamId}", (int)response.StatusCode, steamId64);

        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            throw new PrivateInventoryException(steamId64);
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<SteamInventoryResponse>(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Inventory payload: success={Success}, totalCount={Total}, assets={Assets}, descriptions={Descriptions}, moreItems={More}",
            payload?.Success, payload?.TotalInventoryCount, payload?.Assets?.Count, payload?.Descriptions?.Count, payload?.MoreItems);

        if (payload is null || payload.Success != 1)
        {
            throw new PrivateInventoryException(steamId64);
        }

        return payload;
    }

    private static IReadOnlyList<InventoryItem> JoinAndMap(
        List<SteamAssetDto> assets, List<SteamDescriptionDto> descriptions)
    {
        var descriptionsByKey = new Dictionary<string, SteamDescriptionDto>(descriptions.Count);
        foreach (var description in descriptions)
        {
            descriptionsByKey[JoinKey(description.ClassId, description.InstanceId)] = description;
        }

        var stacks = new Dictionary<string, StackBuilder>(StringComparer.Ordinal);
        foreach (var asset in assets)
        {
            if (!descriptionsByKey.TryGetValue(JoinKey(asset.ClassId, asset.InstanceId), out var description) ||
                string.IsNullOrEmpty(description.MarketHashName))
            {
                continue;
            }

            if (!stacks.TryGetValue(description.MarketHashName, out var stack))
            {
                stack = new StackBuilder(asset, description);
                stacks[description.MarketHashName] = stack;
            }

            stack.Quantity += ParseAmount(asset.Amount);
        }

        return stacks.Values.Select(static stack => stack.Build()).ToList();
    }

    private static string JoinKey(string classId, string instanceId) => $"{classId}_{instanceId}";

    private static int ParseAmount(string amount) =>
        int.TryParse(amount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : 1;

    private sealed class StackBuilder(SteamAssetDto firstAsset, SteamDescriptionDto description)
    {
        public int Quantity { get; set; }

        public InventoryItem Build()
        {
            var weapon = TagValue("Weapon");
            var type = TagValue("Type") ?? description.Type;

            return new InventoryItem
            {
                AssetId = firstAsset.AssetId,
                ClassId = description.ClassId,
                InstanceId = description.InstanceId,
                MarketHashName = description.MarketHashName!,
                Quantity = Quantity,
                Tradable = description.Tradable == 1,
                Marketable = description.Marketable == 1,
                IconUrl = string.IsNullOrEmpty(description.IconUrl) ? null : IconCdnBase + description.IconUrl,
                Weapon = weapon,
                Rarity = SteamTagMapper.MapRarity(TagValue("Rarity")),
                Exterior = SteamTagMapper.MapExterior(TagValue("Exterior")),
                Type = type,
            };
        }

        private string? TagValue(string category) => description.Tags?
            .FirstOrDefault(tag => string.Equals(tag.Category, category, StringComparison.OrdinalIgnoreCase))?
            .DisplayName;
    }
}
