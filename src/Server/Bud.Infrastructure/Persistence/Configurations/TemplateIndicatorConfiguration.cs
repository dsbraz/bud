using Bud.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Infrastructure.Persistence.Configurations;

public sealed class TemplateIndicatorConfiguration : IEntityTypeConfiguration<TemplateIndicator>
{
    public void Configure(EntityTypeBuilder<TemplateIndicator> builder)
    {
        builder.Property(ti => ti.Name)
            .HasMaxLength(200);

        builder.Property(ti => ti.TargetText)
            .HasMaxLength(1000);

        builder.HasOne(ti => ti.Organization)
            .WithMany()
            .HasForeignKey(ti => ti.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ti => ti.TemplateGoal)
            .WithMany(tg => tg.Indicators)
            .HasForeignKey(ti => ti.TemplateGoalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(ti => ti.OrganizationId);
        builder.HasIndex(ti => ti.TemplateId);
        builder.HasIndex(ti => ti.TemplateGoalId);
    }
}
