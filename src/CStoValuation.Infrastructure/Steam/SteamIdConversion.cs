using System.Globalization;

namespace CStoValuation.Infrastructure.Steam;

internal static class SteamIdConversion
{
    private const long SteamId64Base = 76561197960265728L;

    public static string AccountIdToSteamId64(uint accountId) =>
        (SteamId64Base + accountId).ToString(CultureInfo.InvariantCulture);
}
