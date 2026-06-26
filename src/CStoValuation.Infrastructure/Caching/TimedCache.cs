namespace CStoValuation.Infrastructure.Caching;

internal sealed class TimedCache<TValue>
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _timeToLive;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, CacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public TimedCache(TimeProvider timeProvider, TimeSpan timeToLive)
    {
        _timeProvider = timeProvider;
        _timeToLive = timeToLive;
    }

    public async Task<TValue> GetOrAddAsync(
        string key, Func<CancellationToken, Task<TValue>> factory, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        if (TryGetFresh(key, now, out var cached))
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (TryGetFresh(key, now, out cached))
            {
                return cached;
            }

            var fresh = await factory(cancellationToken).ConfigureAwait(false);
            _entries[key] = new CacheEntry(now + _timeToLive, fresh);
            return fresh;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool TryGetFresh(string key, DateTimeOffset now, out TValue value)
    {
        if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAtUtc > now)
        {
            value = entry.Value;
            return true;
        }

        value = default!;
        return false;
    }

    private sealed record CacheEntry(DateTimeOffset ExpiresAtUtc, TValue Value);
}
