namespace CStoValuation.App.ViewModels;

internal sealed class LocalAccountViewModel
{
    public LocalAccountViewModel(string steamId64, string displayName, string? avatarUrl)
    {
        SteamId64 = steamId64;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }

    public string SteamId64 { get; }

    public string DisplayName { get; }

    public string? AvatarUrl { get; }
}
