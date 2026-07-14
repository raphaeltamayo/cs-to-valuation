using CStoValuation.Infrastructure.Steam;
using CStoValuation.Tests.TestSupport;

namespace CStoValuation.Tests;

public class SteamProfileServiceTests
{
    private const string SteamCommunity = "https://steamcommunity.com/";
    private const string SteamId = "76561197960287930";

    [Fact]
    public async Task Parses_the_persona_name_and_avatar_from_the_profile_xml()
    {
        var service = new SteamProfileService(
            MockHttp.ClientReturning(SteamCommunity, Fixtures.Read("steam-profile.xml"), mediaType: "text/xml"));

        var profile = await service.GetProfileAsync(SteamId);

        Assert.NotNull(profile);
        Assert.Equal(SteamId, profile!.SteamId64);
        Assert.Equal("Rabscuttle", profile.PersonaName);
        Assert.Equal("https://avatars.steamstatic.com/abc123_full.jpg", profile.AvatarUrl);
    }

    [Fact]
    public async Task Returns_null_when_the_profile_cannot_be_found()
    {
        var service = new SteamProfileService(
            MockHttp.ClientReturning(SteamCommunity, Fixtures.Read("steam-vanity-notfound.xml"), mediaType: "text/xml"));

        var profile = await service.GetProfileAsync(SteamId);

        Assert.Null(profile);
    }
}
