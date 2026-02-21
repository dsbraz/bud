using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Infrastructure.Persistence.Configurations;

public sealed class MetricCheckinConfiguration : IEntityTypeConfiguration<MetricCheckin>
{
    public void Configure(EntityTypeBuilder<MetricCheckin> builder)
    {
        builder.Property(mc => mc.Note)
            .HasMaxLength(1000);

        builder.Property(mc => mc.Text)
            .HasMaxLength(1000);

        builder.HasOne(mc => mc.Organization)
            .WithMany()
            .HasForeignKey(mc => mc.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mc => mc.OrganizationId);

        builder.HasOne(mc => mc.Collaborator)
            .WithMany()
            .HasForeignKey(mc => mc.CollaboratorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mc => mc.MissionMetricId);
        builder.HasIndex(mc => mc.CollaboratorId);
    }
}
