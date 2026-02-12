using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class CollaboratorTeamConfiguration : IEntityTypeConfiguration<CollaboratorTeam>
{
    public void Configure(EntityTypeBuilder<CollaboratorTeam> builder)
    {
        builder.HasKey(ct => new { ct.CollaboratorId, ct.TeamId });

        builder.HasOne(ct => ct.Collaborator)
            .WithMany(c => c.CollaboratorTeams)
            .HasForeignKey(ct => ct.CollaboratorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.Team)
            .WithMany(t => t.CollaboratorTeams)
            .HasForeignKey(ct => ct.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ct => ct.CollaboratorId);
        builder.HasIndex(ct => ct.TeamId);
    }
}
