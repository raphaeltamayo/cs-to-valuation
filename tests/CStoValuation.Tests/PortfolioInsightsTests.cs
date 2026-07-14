using CStoValuation.Core.Models;
using CStoValuation.Core.Services;
using Xunit;

namespace CStoValuation.Tests;

public class PortfolioInsightsTests
{
    [Fact]
    public void An_empty_inventory_has_no_most_valuable_item()
    {
        Assert.Null(PortfolioInsights.MostValuable([]));
    }

    [Fact]
    public void When_every_line_is_unpriced_there_is_no_most_valuable_item()
    {
        var items = new[] { Unpriced("Sticker | Foo"), Unpriced("Some Case") };

        Assert.Null(PortfolioInsights.MostValuable(items));
    }

    [Fact]
    public void Returns_the_priced_line_with_the_highest_net_value()
    {
        var cheap = Priced("AK-47 | Redline (Field-Tested)", 10.00m);
        var dearest = Priced("AWP | Asiimov (Field-Tested)", 90.00m);
        var middling = Priced("Glock-18 | Fade (Factory New)", 40.00m);

        var result = PortfolioInsights.MostValuable([cheap, dearest, middling]);

        Assert.Same(dearest, result);
    }

    [Fact]
    public void Ignores_unpriced_lines_even_when_they_appear_first()
    {
        var unpriced = Unpriced("Mystery Case");
        var priced = Priced("Karambit | Doppler (Factory New)", 25.00m);

        var result = PortfolioInsights.MostValuable([unpriced, priced]);

        Assert.Same(priced, result);
    }

    private static ValuedItem Priced(string name, decimal lineNet) => new()
    {
        Item = TestData.Item(name),
        Quote = TestData.Quote(name, lineNet),
        LineGross = lineNet,
        LineNet = lineNet,
    };

    private static ValuedItem Unpriced(string name) => new()
    {
        Item = TestData.Item(name),
    };
}
