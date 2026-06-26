namespace CStoValuation.Core.Abstractions;

public interface ISteamIdResolver
{
    Task<string> ResolveAsync(string input, CancellationToken cancellationToken = default);
}
