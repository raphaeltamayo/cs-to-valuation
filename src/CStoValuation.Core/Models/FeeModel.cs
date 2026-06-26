namespace CStoValuation.Core.Models;

public sealed record FeeModel
{
    public static FeeModel Default { get; } = new(0.08m);

    public decimal SellerFeeRate { get; }

    public FeeModel(decimal sellerFeeRate)
    {
        if (sellerFeeRate is < 0m or >= 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sellerFeeRate), sellerFeeRate,
                "Seller fee rate must be in the range [0, 1).");
        }

        SellerFeeRate = sellerFeeRate;
    }

    public decimal NetFromGross(decimal gross) =>
        decimal.Round(gross * (1m - SellerFeeRate), 2, MidpointRounding.AwayFromZero);
}
