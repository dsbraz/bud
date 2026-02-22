using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Infrastructure.Persistence.Configurations;

public sealed class MissionObjectiveConfiguration : IEntityTypeConfiguration<Objective>
{
    public void Configure(EntityTypeBuilder<Objective> builder)
    {
        builder.Property(o => o.Name)
            .HasMaxLength(200);

        builder.Property(o => o.Description)
            .HasMaxLength(1000);

        builder.Property(o => o.Dimension)
            .HasMaxLength(100);

        builder.HasOne(o => o.Organization)
            .WithMany()
            .HasForeignKey(o => o.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.OrganizationId);
        builder.HasIndex(o => o.MissionId);

        builder.HasMany(o => o.Metrics)
            .WithOne(m => m.Objective)
            .HasForeignKey(m => m.ObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.Dimension);
    }
}
