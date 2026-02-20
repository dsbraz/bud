using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Infrastructure.Persistence.Configurations;

public sealed class ObjectiveDimensionConfiguration : IEntityTypeConfiguration<ObjectiveDimension>
{
    public void Configure(EntityTypeBuilder<ObjectiveDimension> builder)
    {
        builder.Property(d => d.Name)
            .HasMaxLength(100);

        builder.HasOne(d => d.Organization)
            .WithMany()
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.OrganizationId);
        builder.HasIndex(d => new { d.OrganizationId, d.Name })
            .IsUnique();
    }
}
