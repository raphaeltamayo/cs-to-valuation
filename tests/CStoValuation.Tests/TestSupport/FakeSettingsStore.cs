using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;

namespace CStoValuation.Tests.TestSupport;

internal sealed class FakeSettingsStore : ISettingsStore
{
    private AppSettings _settings;

    public FakeSettingsStore(AppSettings? settings = null) => _settings = settings ?? AppSettings.Default;

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_settings);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        _settings = settings;
        return Task.CompletedTask;
    }
}
