using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface ICatalogService
{
    Task<IReadOnlyList<CatalogEntry>> GetCatalogAsync(CancellationToken cancellationToken = default);
}
