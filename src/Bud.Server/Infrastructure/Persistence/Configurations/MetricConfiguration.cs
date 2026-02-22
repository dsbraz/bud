using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Infrastructure.Persistence.Configurations;

public sealed class MissionMetricConfiguration : IEntityTypeConfiguration<Metric>
{
    public void Configure(EntityTypeBuilder<Metric> builder)
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

        builder.HasIndex(mm => mm.MissionId);
        builder.HasIndex(mm => mm.ObjectiveId);

        builder.HasMany(mm => mm.Checkins)
            .WithOne(mc => mc.Metric)
            .HasForeignKey(mc => mc.MetricId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
