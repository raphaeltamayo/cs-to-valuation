using CStoValuation.Core.Enums;

namespace CStoValuation.Core.Models;

/// <summary>
/// A single stack of identical items in a Steam inventory, enriched with the
/// descriptive metadata Steam ships alongside the raw assets.
/// </summary>
/// <remarks>
/// Steam returns an inventory as two parallel arrays: <c>assets</c> (what you own
/// and how many) and <c>descriptions</c> (what each thing <i>is</i>). They are
/// joined on <see cref="ClassId"/> + <see cref="InstanceId"/>; this record is the
/// already-joined, domain-friendly result.
/// </remarks>
public sealed record InventoryItem
{
    /// <summary>Unique id of the specific asset instance in the inventory.</summary>
    public required string AssetId { get; init; }

    /// <summary>Class id — identifies the kind of item (half of the join key).</summary>
    public required string ClassId { get; init; }

    /// <summary>Instance id — the other half of the join key ("0" when unused).</summary>
    public required string InstanceId { get; init; }

    /// <summary>
    /// The market hash name, e.g. <c>"AK-47 | Redline (Field-Tested)"</c>. This is
    /// the key every pricing API is queried by, so it is the item's economic identity.
    /// </summary>
    public required string MarketHashName { get; init; }

    /// <summary>How many of this identical item the user holds. Always at least 1.</summary>
    public int Quantity { get; init; } = 1;

    /// <summary>Whether the item can be traded.</summary>
    public bool Tradable { get; init; }

    /// <summary>Whether the item can be listed on the Steam Market.</summary>
    public bool Marketable { get; init; }

    /// <summary>Absolute URL of the item's icon on the Steam CDN, ready to load.</summary>
    public string? IconUrl { get; init; }

    /// <summary>Weapon name from the item's tags, e.g. "AK-47" (open-ended, hence a string).</summary>
    public string? Weapon { get; init; }

    /// <summary>Rarity tier parsed from the item's tags.</summary>
    public Rarity Rarity { get; init; } = Rarity.Unknown;

    /// <summary>Exterior/wear parsed from the item's tags.</summary>
    public Exterior Exterior { get; init; } = Exterior.None;

    /// <summary>Item category from tags, e.g. "Rifle", "Knife", "Container".</summary>
    public string? Type { get; init; }
}
