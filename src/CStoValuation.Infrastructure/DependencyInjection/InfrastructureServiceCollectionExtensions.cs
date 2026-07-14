using System.Net;
using System.Runtime.Versioning;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Services;
using CStoValuation.Infrastructure.Persistence;
using CStoValuation.Infrastructure.Pricing;
using CStoValuation.Infrastructure.Settings;
using CStoValuation.Infrastructure.Skinport;
using CStoValuation.Infrastructure.Steam;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CStoValuation.Infrastructure.DependencyInjection;

[SupportedOSPlatform("windows")]
public static class InfrastructureServiceCollectionExtensions
{
    private const string SteamCommunityBaseUrl = "https://steamcommunity.com/";
    private const string SkinportBaseUrl = "https://api.skinport.com/";
    private const string PriceEmpireBaseUrl = "https://api.pricempire.com/";
    private const string CsFloatBaseUrl = "https://csfloat.com/";
    private const string ExchangeRateBaseUrl = "https://api.frankfurter.app/";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string sqliteConnectionString, string settingsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqliteConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);

        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(sqliteConnectionString));

        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<ISteamSession, SteamSession>();
        services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore(settingsFilePath));
        services.AddSingleton<ISteamAccountLocator, SteamAccountLocator>();

        AddHttpClients(services);

        services.AddSingleton<IValuationService, ValuationService>();

        services.AddSingleton<IInventoryRepository, InventoryRepository>();
        services.AddSingleton<IPriceSnapshotRepository, PriceSnapshotRepository>();

        services.AddSingleton<IPriceProvider, SkinportPriceProvider>();
        services.AddSingleton<IPriceAggregator, PriceAggregator>();

        services.AddHostedService<PriceSnapshotBackgroundService>();

        return services;
    }

    private static void AddHttpClients(IServiceCollection services)
    {
        services.AddHttpClient<ISteamIdResolver, SteamIdResolver>(ConfigureSteamClient);
        services.AddHttpClient<ISteamInventoryService, SteamInventoryService>(ConfigureSteamClient);
        services.AddHttpClient<ISteamMarketPriceService, SteamMarketPriceService>(ConfigureSteamClient);
        services.AddHttpClient<ISteamMarketHistoryService, SteamMarketHistoryService>(ConfigureSteamClient);
        services.AddHttpClient<ISteamProfileService, SteamProfileService>(ConfigureSteamClient);

        services.AddHttpClient<ICsFloatPriceService, CsFloatPriceService>(client =>
        {
            client.BaseAddress = new Uri(CsFloatBaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddHttpClient<IExchangeRateService, ExchangeRateService>(client =>
        {
            client.BaseAddress = new Uri(ExchangeRateBaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddHttpClient(SkinportPriceService.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(SkinportBaseUrl);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate,
            })
            .AddStandardResilienceHandler();

        services.AddSingleton<ISkinportPriceService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            return new SkinportPriceService(
                httpClientFactory.CreateClient(SkinportPriceService.HttpClientName),
                provider.GetRequiredService<TimeProvider>());
        });

        services.AddSingleton<ISkinportSalesHistoryService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            return new SkinportSalesHistoryService(
                httpClientFactory.CreateClient(SkinportPriceService.HttpClientName),
                provider.GetRequiredService<TimeProvider>());
        });

        services.AddHttpClient(PriceEmpirePriceProvider.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(PriceEmpireBaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddSingleton<PriceEmpirePriceProvider>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            return new PriceEmpirePriceProvider(
                httpClientFactory.CreateClient(PriceEmpirePriceProvider.HttpClientName),
                provider.GetRequiredService<ISettingsStore>(),
                provider.GetRequiredService<TimeProvider>());
        });
        services.AddSingleton<IPriceProvider>(provider => provider.GetRequiredService<PriceEmpirePriceProvider>());
    }

    private static void ConfigureSteamClient(HttpClient client)
    {
        client.BaseAddress = new Uri(SteamCommunityBaseUrl);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }
}
