using System.Text.Json.Serialization;

namespace CStoValuation.Infrastructure.Pricing;

internal sealed record PriceEmpireItemDto
{
    [JsonPropertyName("market_hash_name")]
    public string? MarketHashName { get; init; }

    [JsonPropertyName("prices")]
    public List<PriceEmpireProviderPriceDto>? Prices { get; init; }
}

internal sealed record PriceEmpireProviderPriceDto
{
    [JsonPropertyName("provider_key")]
    public string? ProviderKey { get; init; }

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    [JsonPropertyName("count")]
    public int? Count { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
