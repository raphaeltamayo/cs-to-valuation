namespace CStoValuation.Core.Models;

public sealed record ValuedItem
{
    public required InventoryItem Item { get; init; }

    public PriceQuote? Quote { get; init; }

    public decimal LineGross { get; init; }

    public decimal LineNet { get; init; }

    public bool IsPriced => Quote is not null;
}
