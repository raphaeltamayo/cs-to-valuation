using CStoValuation.Infrastructure.Catalog;
using CStoValuation.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace CStoValuation.Tests;

public class CatalogServiceTests : IDisposable
{
    private const string GitHub = "https://raw.githubusercontent.com/";
    private readonly string _cacheFilePath = Path.Combine(Path.GetTempPath(), $"cs2valuator-catalog-{Guid.NewGuid()}.json");

    [Fact]
    public async Task Parses_entries_and_skips_ones_without_a_market_hash_name()
    {
        var service = new CatalogService(
            MockHttp.ClientReturning(GitHub, Fixtures.Read("catalog-skins.json")), _cacheFilePath);

        var entries = await service.GetCatalogAsync();

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, entry => entry.MarketHashName == "AK-47 | Redline (Field-Tested)");
        Assert.Contains(entries, entry => entry.MarketHashName == "AWP | Asiimov (Field-Tested)");
    }

    [Fact]
    public async Task Writes_a_cache_file_that_a_later_instance_can_read_without_a_network_call()
    {
        var firstService = new CatalogService(
            MockHttp.ClientReturning(GitHub, Fixtures.Read("catalog-skins.json")), _cacheFilePath);
        await firstService.GetCatalogAsync();

        Assert.True(File.Exists(_cacheFilePath));

        var secondService = new CatalogService(
            MockHttp.Client(GitHub, _ => throw new InvalidOperationException("no request expected")), _cacheFilePath);

        var entries = await secondService.GetCatalogAsync();

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public async Task Refetches_once_the_cache_is_older_than_a_day()
    {
        var callCount = 0;
        var client = MockHttp.Client(GitHub, _ =>
        {
            callCount++;
            return MockHttp.Response(Fixtures.Read("catalog-skins.json"));
        });
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);

        var firstService = new CatalogService(client, _cacheFilePath, clock);
        await firstService.GetCatalogAsync();
        Assert.Equal(1, callCount);

        clock.Advance(TimeSpan.FromHours(25));
        var secondService = new CatalogService(client, _cacheFilePath, clock);
        await secondService.GetCatalogAsync();

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Falls_back_to_an_empty_list_when_there_is_no_cache_and_the_request_fails()
    {
        var service = new CatalogService(
            MockHttp.ClientWithStatus(GitHub, System.Net.HttpStatusCode.InternalServerError), _cacheFilePath);

        var entries = await service.GetCatalogAsync();

        Assert.Empty(entries);
    }

    public void Dispose()
    {
        if (File.Exists(_cacheFilePath))
        {
            File.Delete(_cacheFilePath);
        }
    }
}
