using CStoValuation.Core.Models;
using Xunit;

namespace CStoValuation.Tests;

public class FeeModelTests
{
    [Fact]
    public void Default_uses_the_standard_eight_percent_skinport_fee()
    {
        Assert.Equal(0.08m, FeeModel.Default.SellerFeeRate);
    }

    [Theory]
    [InlineData(100.00, 0.08, 92.00)]
    [InlineData(100.00, 0.00, 100.00)]
    [InlineData(0.00, 0.08, 0.00)]
    public void NetFromGross_applies_the_fee(decimal gross, decimal rate, decimal expectedNet)
    {
        var model = new FeeModel(rate);

        Assert.Equal(expectedNet, model.NetFromGross(gross));
    }

    [Fact]
    public void NetFromGross_rounds_to_two_decimal_places()
    {
        var model = new FeeModel(0.085m);

        Assert.Equal(30.50m, model.NetFromGross(33.33m));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Constructor_rejects_rates_outside_the_valid_range(decimal invalidRate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FeeModel(invalidRate));
    }

    [Fact]
    public void Constructor_accepts_the_boundary_value_of_zero()
    {
        var model = new FeeModel(0m);

        Assert.Equal(0m, model.SellerFeeRate);
    }
}
