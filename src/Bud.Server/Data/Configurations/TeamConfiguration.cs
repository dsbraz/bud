using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(200);

        builder.HasOne(t => t.Organization)
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.OrganizationId);

        builder.HasMany(t => t.SubTeams)
            .WithOne(t => t.ParentTeam)
            .HasForeignKey(t => t.ParentTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
