using CStoValuation.Core.Enums;
using CStoValuation.Infrastructure.Steam;
using CStoValuation.Tests.TestSupport;

namespace CStoValuation.Tests;

public class SteamMarketPriceServiceTests
{
    private const string SteamCommunity = "https://steamcommunity.com/";

    [Theory]
    [InlineData("1,49€", 1.49)]
    [InlineData("$12.34", 12.34)]
    [InlineData("1.234,56€", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("£0.03", 0.03)]
    public void ParsePrice_handles_locale_formats(string raw, decimal expected)
    {
        Assert.Equal(expected, SteamPriceParser.ParsePrice(raw));
    }

    [Theory]
    [InlineData("1,234", 1234)]
    [InlineData("42", 42)]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void ParseVolume_strips_separators(string? raw, int? expected)
    {
        Assert.Equal(expected, SteamPriceParser.ParseVolume(raw));
    }

    [Fact]
    public async Task Maps_a_successful_overview_to_a_quote()
    {
        const string body = """
            { "success": true, "lowest_price": "12,34€", "median_price": "13,00€", "volume": "1,234" }
            """;
        var service = new SteamMarketPriceService(MockHttp.ClientReturning(SteamCommunity, body));

        var quote = await service.GetPriceOverviewAsync("AK-47 | Redline (Field-Tested)", "EUR");

        Assert.NotNull(quote);
        Assert.Equal(12.34m, quote!.Gross);
        Assert.Equal(1234, quote.Volume);
        Assert.Equal(PriceSource.SteamMarket, quote.Source);
        Assert.Equal("EUR", quote.Currency);
    }

    [Fact]
    public async Task Returns_null_when_the_item_is_not_priced()
    {
        const string body = """{ "success": false }""";
        var service = new SteamMarketPriceService(MockHttp.ClientReturning(SteamCommunity, body));

        var quote = await service.GetPriceOverviewAsync("Nonexistent Item", "EUR");

        Assert.Null(quote);
    }
}
