using CStoValuation.Core.Models;
using CStoValuation.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CStoValuation.Infrastructure.Persistence;

/// <summary>
/// The EF Core unit of work for the app's SQLite database. It owns three tables: the
/// cached inventory, full price snapshots, and the slimmer charting history series.
/// </summary>
/// <remarks>
/// Mapping is configured entirely by Fluent API (see the <c>Configurations</c> folder),
/// applied via <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>. Keeping the
/// configuration out of the entities means the Core POCOs carry no persistence attributes.
/// </remarks>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    internal DbSet<CachedInventoryItem> InventoryItems => Set<CachedInventoryItem>();

    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();

    public DbSet<PriceHistoryPoint> PriceHistoryPoints => Set<PriceHistoryPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Every DateTimeOffset in the model is stored as a sortable Unix-millisecond integer,
        // so SQLite can order and filter time columns (see the converter for the rationale).
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToUnixMillisecondsConverter>();
    }
}
