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

/// <summary>
/// One-stop registration for everything the Infrastructure layer provides. The composition
/// root (the WPF app) just calls <see cref="AddInfrastructure"/>, so wiring details — typed
/// HTTP clients, decompression, resilience, the DbContext factory — stay encapsulated here.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    private const string SteamCommunityBaseUrl = "https://steamcommunity.com/";
    private const string SkinportBaseUrl = "https://api.skinport.com/";

    // A descriptive User-Agent is polite and helps avoid being treated as an anonymous bot.
    private const string UserAgent = "CStoValuation/1.0 (+https://github.com/)";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string sqliteConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqliteConnectionString);

        // A factory (rather than a scoped DbContext) suits a desktop app: the UI and the
        // background snapshot service each create their own short-lived context on demand.
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(sqliteConnectionString));

        // System clock by default; tests substitute a fake. TryAdd keeps a test override intact.
        services.TryAddSingleton(TimeProvider.System);

        AddHttpClients(services);

        // Pure domain logic — no state, safe to share as a singleton.
        services.AddSingleton<IValuationService, ValuationService>();

        // Repositories are stateless (they hold only the context factory), so singletons are fine.
        services.AddSingleton<IInventoryRepository, InventoryRepository>();
        services.AddSingleton<IPriceSnapshotRepository, PriceSnapshotRepository>();

        // Records owned-item prices over time to build the chart/movers history series.
        services.AddHostedService<PriceSnapshotBackgroundService>();

        return services;
    }

    private static void AddHttpClients(IServiceCollection services)
    {
        // Steam community client, shared by the id resolver, inventory, and market services.
        services.AddHttpClient<ISteamIdResolver, SteamIdResolver>(ConfigureSteamClient);
        services.AddHttpClient<ISteamInventoryService, SteamInventoryService>(ConfigureSteamClient);
        services.AddHttpClient<ISteamMarketPriceService, SteamMarketPriceService>(ConfigureSteamClient);

        // Skinport requires Brotli (it returns 406 otherwise), so we configure automatic
        // decompression on the primary handler and add the standard retry/timeout/circuit
        // -breaker resilience pipeline. The service itself is a singleton (its 5-minute
        // cache must outlive a single request), built from a named client.
        services.AddHttpClient(SkinportPriceService.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(SkinportBaseUrl);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
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
    }

    private static void ConfigureSteamClient(HttpClient client)
    {
        client.BaseAddress = new Uri(SteamCommunityBaseUrl);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }
}
