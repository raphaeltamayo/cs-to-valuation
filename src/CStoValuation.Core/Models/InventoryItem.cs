using CStoValuation.Core.Enums;

namespace CStoValuation.Core.Models;

public sealed record InventoryItem
{
    public required string AssetId { get; init; }

    public required string ClassId { get; init; }

    public required string InstanceId { get; init; }

    public required string MarketHashName { get; init; }

    public int Quantity { get; init; } = 1;

    public bool Tradable { get; init; }

    public bool Marketable { get; init; }

    public string? IconUrl { get; init; }

    public string? Weapon { get; init; }

    public Rarity Rarity { get; init; } = Rarity.Unknown;

    public Exterior Exterior { get; init; } = Exterior.None;

    public string? Type { get; init; }
}
