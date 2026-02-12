using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Title)
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .HasMaxLength(1000);

        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(100);

        builder.HasOne(n => n.Organization)
            .WithMany()
            .HasForeignKey(n => n.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.RecipientCollaborator)
            .WithMany()
            .HasForeignKey(n => n.RecipientCollaboratorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.OrganizationId);
        builder.HasIndex(n => new { n.RecipientCollaboratorId, n.IsRead, n.CreatedAtUtc });
    }
}
