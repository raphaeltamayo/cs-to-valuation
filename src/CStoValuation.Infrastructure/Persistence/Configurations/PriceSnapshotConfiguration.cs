using CStoValuation.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CStoValuation.Infrastructure.Persistence.Configurations;

internal sealed class PriceSnapshotConfiguration : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> builder)
    {
        builder.ToTable("PriceSnapshots");
        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.MarketHashName).IsRequired();
        builder.Property(snapshot => snapshot.Currency).IsRequired();

        builder.HasIndex(snapshot => new { snapshot.MarketHashName, snapshot.TakenUtc });
    }
}
