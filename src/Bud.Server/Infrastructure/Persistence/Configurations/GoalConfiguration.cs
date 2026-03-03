using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Infrastructure.Persistence.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.Property(g => g.Name)
            .HasMaxLength(200);

        builder.Property(g => g.Description)
            .HasMaxLength(1000);

        builder.Property(g => g.Dimension)
            .HasMaxLength(100);

        builder.HasOne(g => g.Organization)
            .WithMany()
            .HasForeignKey(g => g.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => g.OrganizationId);

        // Self-referencing relationship for hierarchical goals
        builder.HasOne(g => g.Parent)
            .WithMany(g => g.Children)
            .HasForeignKey(g => g.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => g.ParentId);

        builder.HasOne(g => g.Collaborator)
            .WithMany()
            .HasForeignKey(g => g.CollaboratorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => g.CollaboratorId);

        builder.HasMany(g => g.Indicators)
            .WithOne(i => i.Goal)
            .HasForeignKey(i => i.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Tasks)
            .WithOne(t => t.Goal)
            .HasForeignKey(t => t.GoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
