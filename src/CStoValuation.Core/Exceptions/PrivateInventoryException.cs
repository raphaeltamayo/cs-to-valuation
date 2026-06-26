namespace CStoValuation.Core.Exceptions;

public sealed class PrivateInventoryException : Exception
{
    public string SteamId64 { get; }

    public PrivateInventoryException(string steamId64, string? message = null, Exception? innerException = null)
        : base(message ?? $"The inventory for Steam account {steamId64} is private.", innerException)
    {
        SteamId64 = steamId64;
    }
}
