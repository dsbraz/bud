using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Infrastructure.Persistence.Configurations;

public sealed class CheckinConfiguration : IEntityTypeConfiguration<Checkin>
{
    public void Configure(EntityTypeBuilder<Checkin> builder)
    {
        builder.Property(c => c.Note)
            .HasMaxLength(1000);

        builder.Property(c => c.Text)
            .HasMaxLength(1000);

        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.OrganizationId);

        builder.HasOne(c => c.Collaborator)
            .WithMany()
            .HasForeignKey(c => c.CollaboratorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.IndicatorId);
        builder.HasIndex(c => c.CollaboratorId);
    }
}
