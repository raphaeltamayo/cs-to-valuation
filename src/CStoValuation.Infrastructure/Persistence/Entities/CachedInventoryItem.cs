using CStoValuation.Core.Enums;

namespace CStoValuation.Infrastructure.Persistence.Entities;

internal sealed class CachedInventoryItem
{
    public int Id { get; set; }

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

    public DateTimeOffset CachedAtUtc { get; set; }
}
