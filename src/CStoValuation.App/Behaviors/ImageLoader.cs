using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CStoValuation.App.Behaviors;

/// <summary>
/// Attached behaviour that loads an <see cref="Image.Source"/> from a URL asynchronously,
/// with an in-memory cache, so scrolling a large inventory never blocks the UI thread.
/// </summary>
/// <remarks>
/// Usage in XAML: <c>&lt;Image behaviors:ImageLoader.SourceUrl="{Binding ImageUrl}" /&gt;</c>.
/// Each decoded <see cref="BitmapImage"/> is created with <see cref="BitmapCacheOption.OnLoad"/>
/// (so the source stream can be disposed immediately) and then <c>Freeze()</c>d, which makes it
/// immutable and shareable across the cache and the UI thread without marshalling. Because list
/// virtualization recycles <see cref="Image"/> elements, we re-check the element's current URL
/// before assigning, so a slow download can't land on a row that has since been reused.
/// </remarks>
public static class ImageLoader
{
    private static readonly HttpClient HttpClient = new();
    private static readonly ConcurrentDictionary<string, BitmapImage> Cache = new();

    public static readonly DependencyProperty SourceUrlProperty = DependencyProperty.RegisterAttached(
        "SourceUrl",
        typeof(string),
        typeof(ImageLoader),
        new PropertyMetadata(defaultValue: null, OnSourceUrlChanged));

    public static void SetSourceUrl(DependencyObject element, string? value) =>
        element.SetValue(SourceUrlProperty, value);

    public static string? GetSourceUrl(DependencyObject element) =>
        (string?)element.GetValue(SourceUrlProperty);

    private static async void OnSourceUrlChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not Image image)
        {
            return;
        }

        image.Source = null;
        if (e.NewValue is not string url || string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        var bitmap = await LoadAsync(url);

        // Guard against virtualization recycling: only assign if this element still wants this URL.
        if (bitmap is not null && GetSourceUrl(image) == url)
        {
            image.Source = bitmap;
        }
    }

    private static async Task<BitmapImage?> LoadAsync(string url)
    {
        if (Cache.TryGetValue(url, out var cached))
        {
            return cached;
        }

        try
        {
            var bytes = await HttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            var bitmap = Decode(bytes);
            Cache.TryAdd(url, bitmap);
            return bitmap;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // A missing image is cosmetic — leave the slot blank rather than crash the UI.
            return null;
        }
    }

    private static BitmapImage Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
