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

/// <summary>
/// Application entry point and composition root. A generic <see cref="IHost"/> owns the DI
/// container and service lifetimes — the same hosting model ASP.NET Core uses — so this WPF
/// app gets constructor injection, typed HTTP clients and (later) background services for free.
/// </summary>
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

        // Resolve the main window from the container so its view-model is injected.
        _host.Services.GetRequiredService<MainWindow>().Show();
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

/// <summary>Registration of the app's own services on top of the Infrastructure layer.</summary>
internal static class HostBuilderExtensions
{
    public static HostApplicationBuilder ConfigureServices(this HostApplicationBuilder builder)
    {
        builder.Services.AddInfrastructure($"Data Source={ResolveDatabasePath()}");

        // The interactive Steam OpenID sign-in gesture (shows the WebView2 dialog).
        builder.Services.AddSingleton<ISteamSignIn, SteamOpenIdSignIn>();

        // View-models are transient; the single shell window is a singleton.
        builder.Services.AddTransient<ItemDetailViewModel>();
        builder.Services.AddTransient<MoversViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        return builder;
    }

    /// <summary>
    /// The database lives under %AppData%/CStoValuation so it persists per-user and survives
    /// reinstalls; the directory is created on first run.
    /// </summary>
    private static string ResolveDatabasePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CStoValuation");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "app.db");
    }
}
