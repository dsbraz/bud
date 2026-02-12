using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class MissionConfiguration : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> builder)
    {
        builder.Property(m => m.Name)
            .HasMaxLength(200);

        builder.HasOne(m => m.Organization)
            .WithMany()
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(m => m.OrganizationId);

        builder.HasOne(m => m.Workspace)
            .WithMany()
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Team)
            .WithMany()
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Collaborator)
            .WithMany()
            .HasForeignKey(m => m.CollaboratorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Metrics)
            .WithOne(metric => metric.Mission)
            .HasForeignKey(metric => metric.MissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
