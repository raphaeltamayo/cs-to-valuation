using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CStoValuation.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    internal DbSet<CachedInventoryItem> InventoryItems => Set<CachedInventoryItem>();

    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();

    public DbSet<PriceHistoryPoint> PriceHistoryPoints => Set<PriceHistoryPoint>();

    public DbSet<PortfolioSnapshot> PortfolioSnapshots => Set<PortfolioSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToUnixMillisecondsConverter>();
    }
}
