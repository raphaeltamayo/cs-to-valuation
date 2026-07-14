using CStoValuation.Infrastructure.Steam;
using CStoValuation.Tests.TestSupport;

namespace CStoValuation.Tests;

public class LoginUsersVdfParserTests
{
    [Fact]
    public void Parses_every_account_with_its_fields()
    {
        var accounts = LoginUsersVdfParser.Parse(Fixtures.Read("loginusers.vdf"));

        Assert.Equal(2, accounts.Count);

        var rabscuttle = accounts.Single(account => account.SteamId64 == "76561197960287930");
        Assert.Equal("Rabscuttle", rabscuttle.PersonaName);
        Assert.True(rabscuttle.IsMostRecent);
        Assert.Equal(1750000000, rabscuttle.Timestamp);
        Assert.False(rabscuttle.IsActive);
    }

    [Fact]
    public void Marks_only_the_most_recent_account_as_such()
    {
        var accounts = LoginUsersVdfParser.Parse(Fixtures.Read("loginusers.vdf"));

        var oldAccount = accounts.Single(account => account.SteamId64 == "76561198228458804");
        Assert.False(oldAccount.IsMostRecent);
        Assert.Equal("Some Old Account", oldAccount.PersonaName);
    }

    [Fact]
    public void An_empty_document_yields_no_accounts()
    {
        var accounts = LoginUsersVdfParser.Parse(string.Empty);

        Assert.Empty(accounts);
    }

    [Fact]
    public void A_document_without_a_users_section_yields_no_accounts()
    {
        var accounts = LoginUsersVdfParser.Parse("""
            "somethingElse"
            {
                "key" "value"
            }
            """);

        Assert.Empty(accounts);
    }
}
