namespace CStoValuation.Core.Abstractions;

public interface ISteamSession
{
    string? CookieHeader { get; }

    bool IsAuthenticated { get; }

    void SetCookies(string? cookieHeader);
}
