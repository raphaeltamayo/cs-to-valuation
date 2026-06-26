using CStoValuation.Core.Enums;

namespace CStoValuation.Infrastructure.Steam;

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
