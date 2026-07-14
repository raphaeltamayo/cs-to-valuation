using System.Text.Json.Serialization;

namespace CStoValuation.Infrastructure.Pricing;

internal sealed record CsFloatListingDto
{
    /// <summary>Asking price in US cents.</summary>
    [JsonPropertyName("price")]
    public int? Price { get; init; }
}
