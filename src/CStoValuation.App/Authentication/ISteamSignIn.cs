namespace CStoValuation.App.Authentication;

internal interface ISteamSignIn
{
    Task<string?> SignInAsync();
}
