namespace CStoValuation.Core.Models;

public sealed record LocalSteamAccount(
    string SteamId64,
    string? PersonaName,
    bool IsActive,
    bool IsMostRecent,
    long Timestamp);
