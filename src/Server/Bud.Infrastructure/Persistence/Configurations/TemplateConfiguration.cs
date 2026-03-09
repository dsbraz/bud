using Bud.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Infrastructure.Persistence.Configurations;

public sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.Property(mt => mt.Name)
            .HasMaxLength(200);

        builder.Property(mt => mt.Description)
            .HasMaxLength(1000);

        builder.Property(mt => mt.GoalNamePattern)
            .HasMaxLength(200);

        builder.Property(mt => mt.GoalDescriptionPattern)
            .HasMaxLength(1000);

        builder.HasOne(mt => mt.Organization)
            .WithMany()
            .HasForeignKey(mt => mt.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mt => mt.OrganizationId);

        builder.HasMany(mt => mt.Indicators)
            .WithOne(ti => ti.Template)
            .HasForeignKey(ti => ti.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mt => mt.Goals)
            .WithOne(tg => tg.Template)
            .HasForeignKey(tg => tg.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
