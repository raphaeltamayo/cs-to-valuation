using CStoValuation.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CStoValuation.Infrastructure.Persistence.Configurations;

internal sealed class PortfolioSnapshotConfiguration : IEntityTypeConfiguration<PortfolioSnapshot>
{
    public void Configure(EntityTypeBuilder<PortfolioSnapshot> builder)
    {
        builder.ToTable("PortfolioSnapshots");
        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.Currency).IsRequired();

        builder.HasIndex(snapshot => snapshot.TakenUtc);
    }
}
