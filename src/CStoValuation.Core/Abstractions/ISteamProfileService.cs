using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface ISteamProfileService
{
    Task<SteamUser?> GetProfileAsync(string steamId64, CancellationToken cancellationToken = default);
}
