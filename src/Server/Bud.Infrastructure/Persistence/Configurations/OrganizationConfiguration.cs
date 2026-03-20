using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.Property(o => o.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(o => o.Plan)
            .HasMaxLength(255);

        builder.Property(o => o.IconUrl)
            .HasMaxLength(255);

        builder.Property(o => o.CreatedAt)
            .IsRequired();
    }
}
