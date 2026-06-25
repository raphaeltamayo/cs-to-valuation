using CStoValuation.Core.Enums;

namespace CStoValuation.Infrastructure.Steam;

/// <summary>
/// Maps Steam's English tag values onto the domain's <see cref="Rarity"/> and
/// <see cref="Exterior"/> enums. Mapping on the localized (English) name is more stable
/// than on Steam's internal codes, which vary by item family; anything unrecognised
/// degrades gracefully to <see cref="Rarity.Unknown"/> / <see cref="Exterior.None"/>.
/// </summary>
internal static class SteamTagMapper
{
    private static readonly Dictionary<string, Rarity> RarityByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Consumer Grade"] = Rarity.Consumer,
        ["Industrial Grade"] = Rarity.Industrial,
        ["Mil-Spec Grade"] = Rarity.MilSpec,
        ["Restricted"] = Rarity.Restricted,
        ["Classified"] = Rarity.Classified,
        ["Covert"] = Rarity.Covert,
        ["Contraband"] = Rarity.Contraband,
        ["Extraordinary"] = Rarity.Extraordinary,
    };

    private static readonly Dictionary<string, Exterior> ExteriorByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Factory New"] = Exterior.FactoryNew,
        ["Minimal Wear"] = Exterior.MinimalWear,
        ["Field-Tested"] = Exterior.FieldTested,
        ["Well-Worn"] = Exterior.WellWorn,
        ["Battle-Scarred"] = Exterior.BattleScarred,
    };

    public static Rarity MapRarity(string? localizedName) =>
        localizedName is not null && RarityByName.TryGetValue(localizedName, out var rarity)
            ? rarity
            : Rarity.Unknown;

    public static Exterior MapExterior(string? localizedName) =>
        localizedName is not null && ExteriorByName.TryGetValue(localizedName, out var exterior)
            ? exterior
            : Exterior.None;
}
