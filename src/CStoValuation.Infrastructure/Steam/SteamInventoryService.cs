using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Exceptions;
using CStoValuation.Core.Models;

namespace CStoValuation.Infrastructure.Steam;

/// <summary>
/// Downloads a public CS2 inventory, joins the raw assets to their descriptions, groups
/// identical items into stacks, and maps Steam's tags into clean domain items.
/// </summary>
public sealed class SteamInventoryService : ISteamInventoryService
{
    /// <summary>CDN base that an icon_url path is appended to for a usable image URL.</summary>
    private const string IconCdnBase = "https://community.cloudflare.steamstatic.com/economy/image/";

    private readonly HttpClient _httpClient;

    public SteamInventoryService(HttpClient httpClient) => _httpClient = httpClient;

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(
        string steamId64, CancellationToken cancellationToken = default)
    {
        // 730 = CS2's app id, context 2 = the tradable inventory. count caps the page size.
        var requestUri = $"inventory/{steamId64}/730/2?l=english&count=5000";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

        // A private inventory is an expected, user-fixable condition — surface it as such
        // rather than as a raw HTTP failure.
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new PrivateInventoryException(steamId64);
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<SteamInventoryResponse>(cancellationToken)
            .ConfigureAwait(false);

        if (payload is null || payload.Success != 1)
        {
            throw new PrivateInventoryException(steamId64);
        }

        // success:1 with no assets is a genuinely empty (but public) inventory.
        if (payload.Assets is null || payload.Descriptions is null)
        {
            return [];
        }

        return JoinAndMap(payload.Assets, payload.Descriptions);
    }

    private static IReadOnlyList<InventoryItem> JoinAndMap(
        List<SteamAssetDto> assets, List<SteamDescriptionDto> descriptions)
    {
        // Index the descriptions by the composite join key for O(1) lookup per asset.
        var descriptionsByKey = new Dictionary<string, SteamDescriptionDto>(descriptions.Count);
        foreach (var description in descriptions)
        {
            descriptionsByKey[JoinKey(description.ClassId, description.InstanceId)] = description;
        }

        // Group identical items into stacks keyed by market hash name (their economic
        // identity), summing quantities so the grid shows "AK-47 | Redline ×3".
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

    /// <summary>Accumulates a stack's quantity while remembering the representative asset/description.</summary>
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
