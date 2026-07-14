using CStoValuation.Core.Models;

namespace CStoValuation.Core.Abstractions;

public interface ISteamAccountLocator
{
    IReadOnlyList<LocalSteamAccount> GetLocalAccounts();
}
