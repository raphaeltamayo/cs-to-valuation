using CStoValuation.Core.Enums;

namespace CStoValuation.App.Presentation;

internal static class EnumDisplay
{
    public static string ToLabel(this Rarity rarity) => rarity switch
    {
        Rarity.Consumer => "Consumer",
        Rarity.Industrial => "Industrial",
        Rarity.MilSpec => "Mil-Spec",
        Rarity.Restricted => "Restricted",
        Rarity.Classified => "Classified",
        Rarity.Covert => "Covert",
        Rarity.Contraband => "Contraband",
        Rarity.Extraordinary => "Extraordinary",
        _ => string.Empty,
    };

    public static string ToLabel(this Exterior exterior) => exterior switch
    {
        Exterior.FactoryNew => "Factory New",
        Exterior.MinimalWear => "Minimal Wear",
        Exterior.FieldTested => "Field-Tested",
        Exterior.WellWorn => "Well-Worn",
        Exterior.BattleScarred => "Battle-Scarred",
        _ => string.Empty,
    };
}
