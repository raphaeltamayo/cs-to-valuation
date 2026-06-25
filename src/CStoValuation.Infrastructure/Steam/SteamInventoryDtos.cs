using System.Text.Json.Serialization;

namespace CStoValuation.Infrastructure.Steam;

/// <summary>
/// Wire shape of Steam's inventory endpoint. Steam returns the inventory as two
/// parallel arrays — <see cref="Assets"/> (what is owned) and <see cref="Descriptions"/>
/// (what each thing is) — which the service joins on classid + instanceid.
/// </summary>
internal sealed record SteamInventoryResponse
{
    /// <summary>1 on success; 0 (or a 403 status) typically means the inventory is private.</summary>
    [JsonPropertyName("success")]
    public int Success { get; init; }

    [JsonPropertyName("assets")]
    public List<SteamAssetDto>? Assets { get; init; }

    [JsonPropertyName("descriptions")]
    public List<SteamDescriptionDto>? Descriptions { get; init; }

    [JsonPropertyName("total_inventory_count")]
    public int TotalInventoryCount { get; init; }
}

/// <summary>One owned asset: identifies an item kind and how many are held.</summary>
internal sealed record SteamAssetDto
{
    [JsonPropertyName("classid")]
    public string ClassId { get; init; } = string.Empty;

    [JsonPropertyName("instanceid")]
    public string InstanceId { get; init; } = string.Empty;

    [JsonPropertyName("assetid")]
    public string AssetId { get; init; } = string.Empty;

    /// <summary>Stack size as a string (Steam quirk); "1" for most skins.</summary>
    [JsonPropertyName("amount")]
    public string Amount { get; init; } = "1";
}

/// <summary>The descriptive metadata for an item kind, joined to assets by class+instance.</summary>
internal sealed record SteamDescriptionDto
{
    [JsonPropertyName("classid")]
    public string ClassId { get; init; } = string.Empty;

    [JsonPropertyName("instanceid")]
    public string InstanceId { get; init; } = string.Empty;

    [JsonPropertyName("market_hash_name")]
    public string? MarketHashName { get; init; }

    /// <summary>Steam encodes booleans as 0/1 integers.</summary>
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

/// <summary>A single classification tag (weapon, rarity, exterior, ...).</summary>
internal sealed record SteamTagDto
{
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("internal_name")]
    public string? InternalName { get; init; }

    /// <summary>Human-readable English value (we request <c>l=english</c>).</summary>
    [JsonPropertyName("localized_tag_name")]
    public string? LocalizedTagName { get; init; }

    /// <summary>Some responses use "name" instead of "localized_tag_name".</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The display value, whichever field Steam populated.</summary>
    public string? DisplayName => LocalizedTagName ?? Name;
}
