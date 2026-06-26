using System.Net;
using CStoValuation.Infrastructure.Steam;
using CStoValuation.Tests.TestSupport;

namespace CStoValuation.Tests;

public class SteamIdResolverTests
{
    private const string SteamCommunity = "https://steamcommunity.com/";
    private const string KnownId = "76561197960287930";

    [Fact]
    public async Task A_bare_steamid64_is_returned_without_any_network_call()
    {
        var resolver = new SteamIdResolver(MockHttp.Client(SteamCommunity,
            _ => throw new InvalidOperationException("No network call expected for a bare id.")));

        var result = await resolver.ResolveAsync(KnownId);

        Assert.Equal(KnownId, result);
    }

    [Theory]
    [InlineData("https://steamcommunity.com/profiles/76561197960287930")]
    [InlineData("https://steamcommunity.com/profiles/76561197960287930/")]
    [InlineData("steamcommunity.com/profiles/76561197960287930/")]
    public async Task A_profile_url_yields_the_embedded_id_without_a_network_call(string input)
    {
        var resolver = new SteamIdResolver(MockHttp.Client(SteamCommunity,
            _ => throw new InvalidOperationException("No network call expected for a profile URL.")));

        var result = await resolver.ResolveAsync(input);

        Assert.Equal(KnownId, result);
    }

    [Theory]
    [InlineData("https://steamcommunity.com/id/rabscuttle/")]
    [InlineData("rabscuttle")]
    public async Task A_vanity_url_or_name_is_resolved_via_the_xml_endpoint(string input)
    {
        HttpRequestMessage? captured = null;
        var resolver = new SteamIdResolver(MockHttp.Client(SteamCommunity, request =>
        {
            captured = request;
            return MockHttp.Response(Fixtures.Read("steam-vanity.xml"), mediaType: "text/xml");
        }));

        var result = await resolver.ResolveAsync(input);

        Assert.Equal(KnownId, result);
        Assert.Contains("xml=1", captured!.RequestUri!.ToString());
        Assert.Contains("/id/rabscuttle/", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task An_unknown_vanity_name_throws_a_format_exception()
    {
        var resolver = new SteamIdResolver(MockHttp.Client(SteamCommunity,
            _ => MockHttp.Response(Fixtures.Read("steam-vanity-notfound.xml"), mediaType: "text/xml")));

        await Assert.ThrowsAsync<FormatException>(() => resolver.ResolveAsync("does-not-exist"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_input_throws_a_format_exception(string input)
    {
        var resolver = new SteamIdResolver(MockHttp.ClientWithStatus(SteamCommunity, HttpStatusCode.OK));

        await Assert.ThrowsAsync<FormatException>(() => resolver.ResolveAsync(input));
    }
}
