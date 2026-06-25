using CStoValuation.Core.Enums;

namespace CStoValuation.Infrastructure.Persistence.Entities;

/// <summary>
/// Database row for a cached inventory line. This is the persistence-side counterpart of
/// the domain <see cref="Core.Models.InventoryItem"/>: it adds the owning account and a
/// cache timestamp, and lives in Infrastructure so the domain record stays free of
/// storage concerns (surrogate key, owner foreign key, etc.).
/// </summary>
internal sealed class CachedInventoryItem
{
    public int Id { get; set; }

    /// <summary>The account this cached line belongs to.</summary>
    public string SteamId64 { get; set; } = string.Empty;

    public string AssetId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string MarketHashName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool Tradable { get; set; }
    public bool Marketable { get; set; }
    public string? IconUrl { get; set; }
    public string? Weapon { get; set; }
    public Rarity Rarity { get; set; }
    public Exterior Exterior { get; set; }
    public string? Type { get; set; }

    /// <summary>When this row was written, for staleness checks / display.</summary>
    public DateTimeOffset CachedAtUtc { get; set; }
}
