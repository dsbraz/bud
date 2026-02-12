using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class MissionMetricConfiguration : IEntityTypeConfiguration<MissionMetric>
{
    public void Configure(EntityTypeBuilder<MissionMetric> builder)
    {
        builder.Property(m => m.Name)
            .HasMaxLength(200);

        builder.Property(m => m.TargetText)
            .HasMaxLength(1000);

        builder.HasOne(mm => mm.Organization)
            .WithMany()
            .HasForeignKey(mm => mm.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mm => mm.OrganizationId);

        builder.HasMany(mm => mm.Checkins)
            .WithOne(mc => mc.MissionMetric)
            .HasForeignKey(mc => mc.MissionMetricId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
