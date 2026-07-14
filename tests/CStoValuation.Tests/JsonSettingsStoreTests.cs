using CStoValuation.Core.Enums;
using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Settings;

namespace CStoValuation.Tests;

public class JsonSettingsStoreTests : IDisposable
{
    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"cs2valuator-settings-{Guid.NewGuid()}.json");

    [Fact]
    public async Task Loading_when_no_file_exists_returns_defaults()
    {
        var store = new JsonSettingsStore(_filePath);

        var settings = await store.LoadAsync();

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public async Task Saved_settings_round_trip()
    {
        var store = new JsonSettingsStore(_filePath);
        var settings = AppSettings.Default with
        {
            LastSteamId64 = "76561197960287930",
            PersonaName = "Rabscuttle",
            PrimaryPriceSource = PriceSource.PriceEmpire,
            EnabledPriceSources = [PriceSource.Skinport, PriceSource.PriceEmpire],
            PriceEmpireApiKey = "test-key",
        };

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equal(settings.LastSteamId64, loaded.LastSteamId64);
        Assert.Equal(settings.PersonaName, loaded.PersonaName);
        Assert.Equal(settings.PrimaryPriceSource, loaded.PrimaryPriceSource);
        Assert.Equal(settings.EnabledPriceSources, loaded.EnabledPriceSources);
        Assert.Equal(settings.PriceEmpireApiKey, loaded.PriceEmpireApiKey);
    }

    [Fact]
    public async Task A_corrupt_file_falls_back_to_defaults_instead_of_throwing()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await File.WriteAllTextAsync(_filePath, "{ not valid json");
        var store = new JsonSettingsStore(_filePath);

        var settings = await store.LoadAsync();

        Assert.Equal(AppSettings.Default, settings);
    }

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}
