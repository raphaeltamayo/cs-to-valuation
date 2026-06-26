using CStoValuation.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CStoValuation.Infrastructure.Persistence.Configurations;

internal sealed class CachedInventoryItemConfiguration : IEntityTypeConfiguration<CachedInventoryItem>
{
    public void Configure(EntityTypeBuilder<CachedInventoryItem> builder)
    {
        builder.ToTable("CachedInventoryItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.SteamId64).IsRequired();
        builder.Property(item => item.MarketHashName).IsRequired();

        builder.HasIndex(item => item.SteamId64);
    }
}
