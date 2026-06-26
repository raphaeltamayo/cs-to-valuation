using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using CStoValuation.App.Authentication;
using Microsoft.Web.WebView2.Core;

namespace CStoValuation.App.Views;

internal partial class SteamOpenIdLoginWindow : Window
{
    private static readonly HttpClient HttpClient = new();

    public SteamOpenIdLoginWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public string? SteamId64 { get; private set; }

    public string? CookieHeader { get; private set; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CStoValuation", "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await LoginBrowser.EnsureCoreWebView2Async(environment);

            LoginBrowser.NavigationStarting += OnNavigationStarting;
            LoginBrowser.Source = new Uri(SteamOpenId.BuildLoginUrl());
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(
                this,
                "Couldn't start the embedded browser (the WebView2 runtime may be missing). " +
                "You can still paste your SteamID64 or profile URL instead.",
                "Sign in unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            DialogResult = false;
        }
    }

    private async void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) || !SteamOpenId.IsReturnUrl(uri))
        {
            return;
        }

        e.Cancel = true;

        var steamId = SteamOpenId.ExtractSteamId(uri);
        if (steamId is not null && await SteamOpenId.VerifyAsync(uri, HttpClient, CancellationToken.None))
        {
            SteamId64 = steamId;
            await CaptureSessionCookiesAsync();
            DialogResult = true;
        }
        else
        {
            DialogResult = false;
        }
    }

    private async Task CaptureSessionCookiesAsync()
    {
        try
        {
            var cookies = await LoginBrowser.CoreWebView2.CookieManager
                .GetCookiesAsync("https://steamcommunity.com");
            CookieHeader = string.Join(
                "; ",
                cookies.Where(c => !string.IsNullOrEmpty(c.Value)).Select(c => $"{c.Name}={c.Value}"));
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            CookieHeader = null;
        }
    }
}
