using System.Globalization;

namespace CStoValuation.App.Presentation;

internal static class MoneyFormatter
{
    public const string Placeholder = "—";

    public static string Format(decimal amount, string currency)
    {
        var number = amount.ToString("N2", CultureInfo.CurrentCulture);
        return Symbol(currency) is { } symbol ? $"{symbol}{number}" : $"{number} {currency}";
    }

    public static string Format(decimal? amount, string currency) =>
        amount is { } value ? Format(value, currency) : Placeholder;

    private static string? Symbol(string currency) => currency switch
    {
        "EUR" => "€",
        "USD" => "$",
        "GBP" => "£",
        _ => null,
    };
}
