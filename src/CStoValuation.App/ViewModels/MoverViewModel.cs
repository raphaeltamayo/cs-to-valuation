namespace CStoValuation.App.ViewModels;

internal sealed class MoverViewModel
{
    public MoverViewModel(ValuedItemViewModel ownedItem, string valueText, bool isPositive, string latestPriceText)
    {
        OwnedItem = ownedItem;
        ValueText = valueText;
        IsPositive = isPositive;
        LatestPriceText = latestPriceText;
    }

    public ValuedItemViewModel OwnedItem { get; }

    public string Name => OwnedItem.Name;

    public string? ImageUrl => OwnedItem.ImageUrl;

    public string ValueText { get; }

    public bool IsPositive { get; }

    public string LatestPriceText { get; }
}
