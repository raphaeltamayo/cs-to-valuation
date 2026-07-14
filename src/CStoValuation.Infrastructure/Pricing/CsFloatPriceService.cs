using System.Net.Http.Json;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.Infrastructure.Pricing;

/// <remarks>
/// CSFloat has no bulk price list, so this is queried on demand for a single item (like
/// <see cref="ISteamMarketPriceService"/>), never for a whole-inventory valuation. Prices are
/// always in USD; callers that need another display currency should convert via
/// <see cref="IExchangeRateService"/>. The exact response shape (a bare listing array vs. an
/// envelope) was not verified against a live response while building this.
/// </remarks>
public sealed class CsFloatPriceService : ICsFloatPriceService
{
    public const string HttpClientName = "csfloat";
    private const string UsdCurrency = "USD";

    private readonly HttpClient _httpClient;

    public CsFloatPriceService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<PriceQuote?> GetPriceOverviewAsync(
        string marketHashName, CancellationToken cancellationToken = default)
    {
        var requestUri = $"api/v1/listings?market_hash_name={Uri.EscapeDataString(marketHashName)}&limit=10";
        var listings = await _httpClient
            .GetFromJsonAsync<List<CsFloatListingDto>>(requestUri, cancellationToken)
            .ConfigureAwait(false);

        var cheapest = listings?
            .Where(listing => listing.Price is > 0)
            .MinBy(listing => listing.Price);

        if (cheapest is null)
        {
            return null;
        }

        return new PriceQuote
        {
            MarketHashName = marketHashName,
            Source = PriceSource.CsFloat,
            Currency = UsdCurrency,
            Gross = cheapest.Price!.Value / 100m,
            AsOfUtc = DateTimeOffset.UtcNow,
        };
    }
}
