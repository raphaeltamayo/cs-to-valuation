using System.Xml;
using System.Xml.Linq;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;

namespace CStoValuation.Infrastructure.Steam;

public sealed class SteamProfileService : ISteamProfileService
{
    private readonly HttpClient _httpClient;

    public SteamProfileService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<SteamUser?> GetProfileAsync(string steamId64, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"profiles/{Uri.EscapeDataString(steamId64)}/?xml=1";
            var xml = await _httpClient.GetStringAsync(requestUri, cancellationToken).ConfigureAwait(false);

            var root = XDocument.Parse(xml).Root;
            if (root is null || root.Name != "profile")
            {
                return null;
            }

            var personaName = root.Element("steamID")?.Value;
            var avatarUrl = root.Element("avatarFull")?.Value;
            return new SteamUser(steamId64, personaName, avatarUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or XmlException)
        {
            return null;
        }
    }
}
