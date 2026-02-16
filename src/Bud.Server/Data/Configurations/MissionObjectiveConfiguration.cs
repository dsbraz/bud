using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class MissionObjectiveConfiguration : IEntityTypeConfiguration<MissionObjective>
{
    public void Configure(EntityTypeBuilder<MissionObjective> builder)
    {
        builder.Property(o => o.Name)
            .HasMaxLength(200);

        builder.Property(o => o.Description)
            .HasMaxLength(1000);

        builder.HasOne(o => o.Organization)
            .WithMany()
            .HasForeignKey(o => o.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.OrganizationId);
        builder.HasIndex(o => o.MissionId);

        builder.HasMany(o => o.Metrics)
            .WithOne(m => m.MissionObjective)
            .HasForeignKey(m => m.MissionObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.ObjectiveDimension)
            .WithMany()
            .HasForeignKey(o => o.ObjectiveDimensionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.ObjectiveDimensionId);
    }
}
