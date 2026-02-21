using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Infrastructure.Persistence.Configurations;

public sealed class MissionTemplateMetricConfiguration : IEntityTypeConfiguration<MissionTemplateMetric>
{
    public void Configure(EntityTypeBuilder<MissionTemplateMetric> builder)
    {
        builder.Property(mtm => mtm.Name)
            .HasMaxLength(200);

        builder.Property(mtm => mtm.TargetText)
            .HasMaxLength(1000);

        builder.HasOne(mtm => mtm.Organization)
            .WithMany()
            .HasForeignKey(mtm => mtm.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mtm => mtm.MissionTemplateObjective)
            .WithMany(mto => mto.Metrics)
            .HasForeignKey(mtm => mtm.MissionTemplateObjectiveId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(mtm => mtm.OrganizationId);
        builder.HasIndex(mtm => mtm.MissionTemplateId);
        builder.HasIndex(mtm => mtm.MissionTemplateObjectiveId);
    }
}
