using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface IInventoryRepository
{
    Task SaveInventoryAsync(
        string steamId64, IReadOnlyCollection<InventoryItem> items,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItem>> GetCachedInventoryAsync(
        string steamId64, CancellationToken cancellationToken = default);
}
