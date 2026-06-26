using CStoValuation.App.Presentation;
using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;

namespace CStoValuation.App.ViewModels;

internal sealed class ValuedItemViewModel
{
    public ValuedItemViewModel(ValuedItem valued, string currency)
    {
        var item = valued.Item;

        Name = item.MarketHashName;
        ImageUrl = item.IconUrl;
        Weapon = item.Weapon;
        Type = item.Type;
        Rarity = item.Rarity;
        RarityLabel = item.Rarity.ToLabel();
        HasRarity = item.Rarity != Rarity.Unknown;
        ExteriorLabel = item.Exterior.ToLabel();
        Quantity = item.Quantity;
        IsPriced = valued.IsPriced;

        UnitGross = valued.Quote?.Gross;
        UnitNet = valued.IsPriced ? valued.LineNet / item.Quantity : null;
        LineGross = valued.IsPriced ? valued.LineGross : null;
        LineNet = valued.IsPriced ? valued.LineNet : null;

        UnitGrossText = MoneyFormatter.Format(UnitGross, currency);
        UnitNetText = MoneyFormatter.Format(UnitNet, currency);
        LineGrossText = MoneyFormatter.Format(LineGross, currency);
        LineNetText = MoneyFormatter.Format(LineNet, currency);
    }

    public string Name { get; }
    public string? ImageUrl { get; }
    public string? Weapon { get; }
    public string? Type { get; }
    public Rarity Rarity { get; }
    public string RarityLabel { get; }
    public bool HasRarity { get; }
    public string ExteriorLabel { get; }
    public int Quantity { get; }
    public bool IsPriced { get; }

    public decimal? UnitGross { get; }
    public decimal? UnitNet { get; }
    public decimal? LineGross { get; }
    public decimal? LineNet { get; }

    public string UnitGrossText { get; }
    public string UnitNetText { get; }
    public string LineGrossText { get; }
    public string LineNetText { get; }
}
