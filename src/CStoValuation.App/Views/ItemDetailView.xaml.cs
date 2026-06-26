using System.Windows.Controls;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CStoValuation.App.Views;

public partial class ItemDetailView : UserControl
{
    public ItemDetailView()
    {
        InitializeComponent();

        TrendChart.TooltipBackgroundPaint = new SolidColorPaint(SKColor.Parse("#272B33"));
        TrendChart.TooltipTextPaint = new SolidColorPaint(SKColor.Parse("#E7E9EE")) { SKTypeface = SKTypeface.Default };
    }
}
