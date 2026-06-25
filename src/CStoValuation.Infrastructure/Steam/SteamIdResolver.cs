using System.Text.RegularExpressions;
using System.Xml.Linq;
using CStoValuation.Core.Abstractions;

namespace CStoValuation.Infrastructure.Steam;

/// <summary>
/// Resolves the many ways a user can identify a Steam account down to a canonical
/// SteamID64. Order of attempts: a bare SteamID64, then a <c>/profiles/{id}/</c> URL
/// (the id is already in the path), then a <c>/id/{vanity}/</c> custom URL or a bare
/// vanity name, which requires a network lookup against Steam's <c>?xml=1</c> endpoint.
/// </summary>
public sealed partial class SteamIdResolver : ISteamIdResolver
{
    private readonly HttpClient _httpClient;

    public SteamIdResolver(HttpClient httpClient) => _httpClient = httpClient;

    // [GeneratedRegex] compiles these patterns at build time into purpose-built code,
    // so there is no runtime regex parsing cost and the patterns are validated by the
    // compiler. A SteamID64 always starts with the 7656119 prefix and is 17 digits.
    [GeneratedRegex(@"^7656119\d{10}$")]
    private static partial Regex SteamId64Pattern();

    [GeneratedRegex(@"/profiles/(7656119\d{10})")]
    private static partial Regex ProfileUrlPattern();

    [GeneratedRegex(@"/id/([^/?#]+)")]
    private static partial Regex VanityUrlPattern();

    /// <inheritdoc />
    public async Task<string> ResolveAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new FormatException("No Steam ID or profile URL was provided.");
        }

        var trimmed = input.Trim();

        // 1. Already a canonical id — nothing to do.
        if (SteamId64Pattern().IsMatch(trimmed))
        {
            return trimmed;
        }

        // 2. A /profiles/{id}/ URL carries the id directly.
        var profileMatch = ProfileUrlPattern().Match(trimmed);
        if (profileMatch.Success)
        {
            return profileMatch.Groups[1].Value;
        }

        // 3. A /id/{vanity}/ URL, or a bare vanity name, needs a lookup.
        var vanityMatch = VanityUrlPattern().Match(trimmed);
        var vanity = vanityMatch.Success ? vanityMatch.Groups[1].Value : trimmed;
        return await ResolveVanityAsync(vanity, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ResolveVanityAsync(string vanity, CancellationToken cancellationToken)
    {
        // The ?xml=1 view of a profile contains a <steamID64> element and needs no API key.
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
