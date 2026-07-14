using CStoValuation.Infrastructure.Steam;

namespace CStoValuation.Tests;

public class SteamIdConversionTests
{
    [Theory]
    [InlineData(0u, "76561197960265728")]
    [InlineData(1u, "76561197960265729")]
    [InlineData(22202u, "76561197960287930")]
    public void Converts_a_32_bit_account_id_to_the_matching_steamid64(uint accountId, string expectedSteamId64)
    {
        Assert.Equal(expectedSteamId64, SteamIdConversion.AccountIdToSteamId64(accountId));
    }
}
