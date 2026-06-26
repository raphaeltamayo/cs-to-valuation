using CStoValuation.Core.Abstractions;

namespace CStoValuation.Infrastructure.Steam;

public sealed class SteamSession : ISteamSession
{
    private volatile string? _cookieHeader;

    public string? CookieHeader => _cookieHeader;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_cookieHeader);

    public void SetCookies(string? cookieHeader) =>
        _cookieHeader = string.IsNullOrWhiteSpace(cookieHeader) ? null : cookieHeader;
}
