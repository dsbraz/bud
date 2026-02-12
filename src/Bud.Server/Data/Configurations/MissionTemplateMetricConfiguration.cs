using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

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

        builder.HasIndex(mtm => mtm.OrganizationId);
    }
}
