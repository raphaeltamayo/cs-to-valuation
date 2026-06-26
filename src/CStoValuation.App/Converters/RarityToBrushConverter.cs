using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CStoValuation.Core.Enums;

namespace CStoValuation.App.Converters;

/// <summary>
/// Maps a <see cref="Rarity"/> to the colour CS2 uses for that tier, for the little rarity
/// chip in each row. Returns a frozen brush (immutable, freely shareable across threads and
/// rows). An <see cref="IValueConverter"/> is the WPF equivalent of a JavaFX cell factory or
/// a JSF converter: a small adapter between a data value and its visual representation.
/// </summary>
public sealed class RarityToBrushConverter : IValueConverter
{
    private static readonly IReadOnlyDictionary<Rarity, Brush> Brushes = new Dictionary<Rarity, Brush>
    {
        [Rarity.Consumer] = Frozen("#B0C3D9"),
        [Rarity.Industrial] = Frozen("#5E98D9"),
        [Rarity.MilSpec] = Frozen("#4B69FF"),
        [Rarity.Restricted] = Frozen("#8847FF"),
        [Rarity.Classified] = Frozen("#D32CE6"),
        [Rarity.Covert] = Frozen("#EB4B4B"),
        [Rarity.Contraband] = Frozen("#E4AE39"),
        [Rarity.Extraordinary] = Frozen("#FFD700"),
        [Rarity.Unknown] = Frozen("#6C7079"),
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Rarity rarity && Brushes.TryGetValue(rarity, out var brush)
            ? brush
            : Brushes[Rarity.Unknown];

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
