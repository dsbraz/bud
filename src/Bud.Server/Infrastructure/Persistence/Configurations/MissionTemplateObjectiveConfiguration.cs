using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Infrastructure.Persistence.Configurations;

public sealed class MissionTemplateObjectiveConfiguration : IEntityTypeConfiguration<MissionTemplateObjective>
{
    public void Configure(EntityTypeBuilder<MissionTemplateObjective> builder)
    {
        builder.Property(mto => mto.Name)
            .HasMaxLength(200);

        builder.Property(mto => mto.Description)
            .HasMaxLength(1000);

        builder.HasOne(mto => mto.Organization)
            .WithMany()
            .HasForeignKey(mto => mto.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(mto => mto.ObjectiveDimension)
            .WithMany()
            .HasForeignKey(mto => mto.ObjectiveDimensionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mto => mto.OrganizationId);
        builder.HasIndex(mto => mto.MissionTemplateId);
    }
}
