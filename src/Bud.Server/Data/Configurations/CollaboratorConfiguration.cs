using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class CollaboratorConfiguration : IEntityTypeConfiguration<Collaborator>
{
    public void Configure(EntityTypeBuilder<Collaborator> builder)
    {
        builder.Property(c => c.FullName)
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .HasMaxLength(320);

        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.OrganizationId);

        builder.HasOne(c => c.Team)
            .WithMany(t => t.Collaborators)
            .HasForeignKey(c => c.TeamId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(c => c.Leader)
            .WithMany()
            .HasForeignKey(c => c.LeaderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(c => c.Email)
            .IsUnique();
    }
}
