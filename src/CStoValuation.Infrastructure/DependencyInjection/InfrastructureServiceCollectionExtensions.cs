using System.Net;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Services;
using CStoValuation.Infrastructure.Persistence;
using CStoValuation.Infrastructure.Pricing;
using CStoValuation.Infrastructure.Skinport;
using CStoValuation.Infrastructure.Steam;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CStoValuation.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    private const string SteamCommunityBaseUrl = "https://steamcommunity.com/";
    private const string SkinportBaseUrl = "https://api.skinport.com/";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string sqliteConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqliteConnectionString);

        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(sqliteConnectionString));

        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<ISteamSession, SteamSession>();

        AddHttpClients(services);

        services.AddSingleton<IValuationService, ValuationService>();

        services.AddSingleton<IInventoryRepository, InventoryRepository>();
        services.AddSingleton<IPriceSnapshotRepository, PriceSnapshotRepository>();

        services.AddHostedService<PriceSnapshotBackgroundService>();

        return services;
    }

    private static void AddHttpClients(IServiceCollection services)
    {
        services.AddHttpClient<ISteamIdResolver, SteamIdResolver>(ConfigureSteamClient);
        services.AddHttpClient<ISteamInventoryService, SteamInventoryService>(ConfigureSteamClient);
        services.AddHttpClient<ISteamMarketPriceService, SteamMarketPriceService>(ConfigureSteamClient);
        services.AddHttpClient<ISteamMarketHistoryService, SteamMarketHistoryService>(ConfigureSteamClient);

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
    }

    private static void ConfigureSteamClient(HttpClient client)
    {
        client.BaseAddress = new Uri(SteamCommunityBaseUrl);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }
}
