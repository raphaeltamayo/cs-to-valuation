namespace CStoValuation.App.ViewModels;

internal sealed class MoverViewModel
{
    public MoverViewModel(string name, string valueText, bool isPositive, string latestPriceText)
    {
        Name = name;
        ValueText = valueText;
        IsPositive = isPositive;
        LatestPriceText = latestPriceText;
    }

    public string Name { get; }
    public string ValueText { get; }
    public bool IsPositive { get; }
    public string LatestPriceText { get; }
}
