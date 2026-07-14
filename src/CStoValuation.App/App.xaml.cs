using System.IO;
using System.Windows;
using CStoValuation.App.Authentication;
using CStoValuation.App.ViewModels;
using CStoValuation.App.Views;
using CStoValuation.Infrastructure.DependencyInjection;
using CStoValuation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CStoValuation.App;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateApplicationBuilder()
            .ConfigureServices()
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await _host.StartAsync();
        await MigrateDatabaseAsync();

        _host.Services.GetRequiredService<MainWindow>().Show();

        _ = _host.Services.GetRequiredService<InventoryPageViewModel>().InitializeAsync();
        _ = _host.Services.GetRequiredService<SettingsPageViewModel>().InitializeAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    private async Task MigrateDatabaseAsync()
    {
        var factory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }
}

internal static class HostBuilderExtensions
{
    public static HostApplicationBuilder ConfigureServices(this HostApplicationBuilder builder)
    {
        var appDataFolder = ResolveAppDataFolder();
        builder.Services.AddInfrastructure(
            $"Data Source={Path.Combine(appDataFolder, "app.db")}",
            Path.Combine(appDataFolder, "settings.json"));

        builder.Services.AddSingleton<ISteamSignIn, SteamOpenIdSignIn>();

        builder.Services.AddTransient<ItemDetailViewModel>();
        builder.Services.AddTransient<MoversViewModel>();
        builder.Services.AddSingleton<InventoryPageViewModel>();
        builder.Services.AddSingleton<CatalogPageViewModel>();
        builder.Services.AddSingleton<PerformancePageViewModel>();
        builder.Services.AddSingleton<SettingsPageViewModel>();
        builder.Services.AddSingleton<ShellViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        return builder;
    }

    private static string ResolveAppDataFolder()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CStoValuation");
        Directory.CreateDirectory(folder);
        return folder;
    }
}
