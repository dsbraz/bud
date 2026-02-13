using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class CollaboratorAccessLogConfiguration : IEntityTypeConfiguration<CollaboratorAccessLog>
{
    public void Configure(EntityTypeBuilder<CollaboratorAccessLog> builder)
    {
        builder.HasOne(cal => cal.Organization)
            .WithMany()
            .HasForeignKey(cal => cal.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cal => cal.Collaborator)
            .WithMany()
            .HasForeignKey(cal => cal.CollaboratorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cal => new { cal.OrganizationId, cal.AccessedAt });
    }
}
