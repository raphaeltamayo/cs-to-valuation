using System.Text.Json.Serialization;

namespace CStoValuation.Infrastructure.Skinport;

/// <summary>
/// Wire shape of a single entry in Skinport's <c>/v1/items</c> response. This is a
/// Data Transfer Object: it exists only to deserialize JSON and is mapped to the
/// domain <see cref="Core.Models.PriceQuote"/> immediately, so Skinport's naming and
/// nullability never leak past the Infrastructure boundary.
/// </summary>
internal sealed record SkinportItemDto
{
    [JsonPropertyName("market_hash_name")]
    public string? MarketHashName { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    /// <summary>Lowest listing price; <c>null</c> when nothing is currently listed.</summary>
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

    /// <summary>Number of items currently listed — a liquidity signal.</summary>
    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }

    /// <summary>Unix epoch seconds of the last update.</summary>
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; init; }
}
