using CStoValuation.Core.Enums;
using CStoValuation.Infrastructure.Pricing;
using CStoValuation.Tests.TestSupport;

namespace CStoValuation.Tests;

public class CsFloatPriceServiceTests
{
    private const string CsFloat = "https://csfloat.com/";
    private const string Item = "AK-47 | Redline (Field-Tested)";

    [Fact]
    public async Task Converts_the_cheapest_listing_from_cents_to_a_decimal_usd_amount()
    {
        var service = new CsFloatPriceService(
            MockHttp.ClientReturning(CsFloat, Fixtures.Read("csfloat-listings.json")));

        var quote = await service.GetPriceOverviewAsync(Item);

        Assert.NotNull(quote);
        Assert.Equal(12.34m, quote!.Gross);
        Assert.Equal("USD", quote.Currency);
        Assert.Equal(PriceSource.CsFloat, quote.Source);
    }

    [Fact]
    public async Task Returns_null_when_there_are_no_listings()
    {
        var service = new CsFloatPriceService(MockHttp.ClientReturning(CsFloat, "[]"));

        var quote = await service.GetPriceOverviewAsync(Item);

        Assert.Null(quote);
    }

    [Fact]
    public async Task Propagates_the_failure_when_the_request_fails()
    {
        var service = new CsFloatPriceService(
            MockHttp.ClientWithStatus(CsFloat, System.Net.HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetPriceOverviewAsync(Item));
    }
}
