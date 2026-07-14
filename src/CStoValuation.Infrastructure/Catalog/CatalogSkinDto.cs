using System.Text.Json.Serialization;

namespace CStoValuation.Infrastructure.Catalog;

internal sealed record CatalogSkinDto
{
    [JsonPropertyName("market_hash_name")]
    public string? MarketHashName { get; init; }

    [JsonPropertyName("image")]
    public string? Image { get; init; }
}
