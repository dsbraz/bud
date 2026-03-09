using Bud.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Infrastructure.Persistence.Configurations;

public sealed class IndicatorConfiguration : IEntityTypeConfiguration<Indicator>
{
    public void Configure(EntityTypeBuilder<Indicator> builder)
    {
        builder.Property(i => i.Name)
            .HasMaxLength(200);

        builder.Property(i => i.TargetText)
            .HasMaxLength(1000);

        builder.HasOne(i => i.Organization)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.OrganizationId);
        builder.HasIndex(i => i.GoalId);

        builder.HasMany(i => i.Checkins)
            .WithOne(c => c.Indicator)
            .HasForeignKey(c => c.IndicatorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
