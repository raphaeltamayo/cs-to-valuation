using System.Globalization;
using System.Text;

namespace CStoValuation.Infrastructure.Steam;

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
