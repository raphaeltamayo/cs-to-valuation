using System.Text.RegularExpressions;
using System.Xml.Linq;
using CStoValuation.Core.Abstractions;

namespace CStoValuation.Infrastructure.Steam;

public sealed partial class SteamIdResolver : ISteamIdResolver
{
    private readonly HttpClient _httpClient;

    public SteamIdResolver(HttpClient httpClient) => _httpClient = httpClient;

    [GeneratedRegex(@"^7656119\d{10}$")]
    private static partial Regex SteamId64Pattern();

    [GeneratedRegex(@"/profiles/(7656119\d{10})")]
    private static partial Regex ProfileUrlPattern();

    [GeneratedRegex(@"/id/([^/?#]+)")]
    private static partial Regex VanityUrlPattern();

    public async Task<string> ResolveAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new FormatException("No Steam ID or profile URL was provided.");
        }

        var trimmed = input.Trim();

        if (SteamId64Pattern().IsMatch(trimmed))
        {
            return trimmed;
        }

        var profileMatch = ProfileUrlPattern().Match(trimmed);
        if (profileMatch.Success)
        {
            return profileMatch.Groups[1].Value;
        }

        var vanityMatch = VanityUrlPattern().Match(trimmed);
        var vanity = vanityMatch.Success ? vanityMatch.Groups[1].Value : trimmed;
        return await ResolveVanityAsync(vanity, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ResolveVanityAsync(string vanity, CancellationToken cancellationToken)
    {
        var requestUri = $"id/{Uri.EscapeDataString(vanity)}/?xml=1";
        var xml = await _httpClient.GetStringAsync(requestUri, cancellationToken).ConfigureAwait(false);

        var steamId = XDocument.Parse(xml).Root?.Element("steamID64")?.Value;
        if (string.IsNullOrEmpty(steamId) || !SteamId64Pattern().IsMatch(steamId))
        {
            throw new FormatException($"Could not resolve '{vanity}' to a SteamID64.");
        }

        return steamId;
    }
}
