using CStoValuation.Infrastructure.Pricing;
using CStoValuation.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace CStoValuation.Tests;

public class ExchangeRateServiceTests
{
    private const string ExchangeRates = "https://api.frankfurter.app/";

    [Fact]
    public async Task Returns_the_amount_unchanged_when_currencies_match()
    {
        var service = new ExchangeRateService(
            MockHttp.Client(ExchangeRates, _ => throw new InvalidOperationException("no request expected")));

        var converted = await service.ConvertAsync(100m, "EUR", "eur");

        Assert.Equal(100m, converted);
    }

    [Fact]
    public async Task Converts_using_the_reported_rate()
    {
        var service = new ExchangeRateService(
            MockHttp.ClientReturning(ExchangeRates, """{ "amount": 1.0, "base": "USD", "date": "2026-01-01", "rates": { "EUR": 0.92 } }"""));

        var converted = await service.ConvertAsync(10m, "USD", "EUR");

        Assert.Equal(9.20m, converted);
    }

    [Fact]
    public async Task Propagates_the_failure_when_the_request_fails()
    {
        var service = new ExchangeRateService(
            MockHttp.ClientWithStatus(ExchangeRates, System.Net.HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.ConvertAsync(10m, "USD", "EUR"));
    }

    [Fact]
    public async Task Throws_when_the_response_is_missing_the_requested_currency()
    {
        var service = new ExchangeRateService(
            MockHttp.ClientReturning(ExchangeRates, """{ "amount": 1.0, "base": "USD", "date": "2026-01-01", "rates": { "GBP": 0.79 } }"""));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConvertAsync(10m, "USD", "EUR"));
    }

    [Fact]
    public async Task Caches_the_rate_for_twenty_four_hours()
    {
        var callCount = 0;
        var client = MockHttp.Client(ExchangeRates, _ =>
        {
            callCount++;
            return MockHttp.Response("""{ "amount": 1.0, "base": "USD", "date": "2026-01-01", "rates": { "EUR": 0.92 } }""");
        });
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var service = new ExchangeRateService(client, clock);

        await service.ConvertAsync(10m, "USD", "EUR");
        await service.ConvertAsync(20m, "USD", "EUR");
        Assert.Equal(1, callCount);

        clock.Advance(TimeSpan.FromHours(25));
        await service.ConvertAsync(10m, "USD", "EUR");
        Assert.Equal(2, callCount);
    }
}
