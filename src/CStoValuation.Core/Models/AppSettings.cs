using CStoValuation.Core.Enums;

namespace CStoValuation.Core.Models;

public sealed record AppSettings
{
    public string? LastSteamId64 { get; init; }

    public string? PersonaName { get; init; }

    public string? AvatarUrl { get; init; }

    public string Currency { get; init; } = "EUR";

    public PriceSource PrimaryPriceSource { get; init; } = PriceSource.Skinport;

    public IReadOnlyList<PriceSource> EnabledPriceSources { get; init; } = [PriceSource.Skinport];

    public string? PriceEmpireApiKey { get; init; }

    public static AppSettings Default { get; } = new();
}
