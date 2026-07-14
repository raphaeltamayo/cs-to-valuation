using CStoValuation.App.Presentation;

namespace CStoValuation.App.ViewModels;

internal sealed class CatalogItemViewModel
{
    public CatalogItemViewModel(string name, string? imageUrl, decimal? gross, string currency)
    {
        Name = name;
        ImageUrl = imageUrl;
        Gross = gross;
        Currency = currency;
        PriceText = MoneyFormatter.Format(gross, currency);
    }

    public string Name { get; }

    public string? ImageUrl { get; }

    public decimal? Gross { get; }

    public string Currency { get; }

    public string PriceText { get; }
}
