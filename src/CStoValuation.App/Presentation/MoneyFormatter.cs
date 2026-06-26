using System.Globalization;

namespace CStoValuation.App.Presentation;

/// <summary>
/// Formats monetary amounts for display. Kept in one place so every screen renders money
/// the same way, and so the "—" placeholder for an unpriced value is defined exactly once.
/// </summary>
internal static class MoneyFormatter
{
    /// <summary>Shown wherever a value is unknown (no price was found).</summary>
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
