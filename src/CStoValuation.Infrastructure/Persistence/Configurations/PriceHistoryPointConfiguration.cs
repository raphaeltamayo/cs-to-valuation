using CStoValuation.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CStoValuation.Infrastructure.Persistence.Configurations;

internal sealed class PriceHistoryPointConfiguration : IEntityTypeConfiguration<PriceHistoryPoint>
{
    public void Configure(EntityTypeBuilder<PriceHistoryPoint> builder)
    {
        builder.ToTable("PriceHistoryPoints");
        builder.HasKey(point => point.Id);

        builder.Property(point => point.MarketHashName).IsRequired();

        // The chart reads one item's series in time order; this composite index serves it.
        builder.HasIndex(point => new { point.MarketHashName, point.DateUtc });
    }
}
