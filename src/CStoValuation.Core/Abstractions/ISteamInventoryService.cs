using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface ISteamInventoryService
{
    Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(
        string steamId64, CancellationToken cancellationToken = default);
}
