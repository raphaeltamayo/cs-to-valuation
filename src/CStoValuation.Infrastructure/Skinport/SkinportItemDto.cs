using System.Text.Json.Serialization;

namespace CStoValuation.Infrastructure.Skinport;

internal sealed record SkinportItemDto
{
    [JsonPropertyName("market_hash_name")]
    public string? MarketHashName { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; init; }

    [JsonPropertyName("max_price")]
    public decimal? MaxPrice { get; init; }

    [JsonPropertyName("mean_price")]
    public decimal? MeanPrice { get; init; }

    [JsonPropertyName("median_price")]
    public decimal? MedianPrice { get; init; }

    [JsonPropertyName("suggested_price")]
    public decimal? SuggestedPrice { get; init; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }

    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; init; }
}
