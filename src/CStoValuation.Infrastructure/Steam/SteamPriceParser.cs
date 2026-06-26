using System.Globalization;
using System.Text;

namespace CStoValuation.Infrastructure.Steam;

/// <summary>
/// Parses the locale-formatted price and volume strings the Steam Market returns (e.g.
/// "1.234,56€", "$12.34", "1,234"). Rather than guess a culture, we normalise: the last
/// ',' or '.' is treated as the decimal separator and every other separator is dropped,
/// which handles both European and US formatting from one code path.
/// </summary>
internal static class SteamPriceParser
{
    public static decimal? ParsePrice(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = KeepDigitsAndSeparators(raw);
        if (digits.Length == 0)
        {
            return null;
        }

        var lastComma = digits.LastIndexOf(',');
        var lastDot = digits.LastIndexOf('.');
        var decimalSeparator = Math.Max(lastComma, lastDot);

        string normalized;
        if (decimalSeparator < 0)
        {
            normalized = digits;
        }
        else
        {
            // Strip all separators, then reinsert a single '.' at the decimal position.
            var integerPart = digits[..decimalSeparator].Replace(",", string.Empty).Replace(".", string.Empty);
            var fractionPart = digits[(decimalSeparator + 1)..].Replace(",", string.Empty).Replace(".", string.Empty);
            normalized = $"{integerPart}.{fractionPart}";
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    public static int? ParseVolume(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        // Volume is a whole number; keep only digits (drops thousands separators).
        var builder = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsDigit(c))
            {
                builder.Append(c);
            }
        }

        return builder.Length > 0 && int.TryParse(builder.ToString(), out var value) ? value : null;
    }

    private static string KeepDigitsAndSeparators(string raw)
    {
        var builder = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsDigit(c) || c is ',' or '.')
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
