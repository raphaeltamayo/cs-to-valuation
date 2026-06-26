using System.Text.Json.Serialization;

namespace CStoValuation.Infrastructure.Steam;

internal sealed record SteamInventoryResponse
{
    [JsonPropertyName("success")]
    public int Success { get; init; }

    [JsonPropertyName("assets")]
    public List<SteamAssetDto>? Assets { get; init; }

    [JsonPropertyName("descriptions")]
    public List<SteamDescriptionDto>? Descriptions { get; init; }

    [JsonPropertyName("total_inventory_count")]
    public int TotalInventoryCount { get; init; }

    [JsonPropertyName("more_items")]
    public int MoreItems { get; init; }

    [JsonPropertyName("last_assetid")]
    public string? LastAssetId { get; init; }
}

internal sealed record SteamAssetDto
{
    [JsonPropertyName("classid")]
    public string ClassId { get; init; } = string.Empty;

    [JsonPropertyName("instanceid")]
    public string InstanceId { get; init; } = string.Empty;

    [JsonPropertyName("assetid")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; init; } = "1";
}

internal sealed record SteamDescriptionDto
{
    [JsonPropertyName("classid")]
    public string ClassId { get; init; } = string.Empty;

    [JsonPropertyName("instanceid")]
    public string InstanceId { get; init; } = string.Empty;

    [JsonPropertyName("market_hash_name")]
    public string? MarketHashName { get; init; }

    [JsonPropertyName("tradable")]
    public int Tradable { get; init; }

    [JsonPropertyName("marketable")]
    public int Marketable { get; init; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("tags")]
    public List<SteamTagDto>? Tags { get; init; }
}

internal sealed record SteamTagDto
{
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("internal_name")]
    public string? InternalName { get; init; }

    [JsonPropertyName("localized_tag_name")]
    public string? LocalizedTagName { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    public string? DisplayName => LocalizedTagName ?? Name;
}
