using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class MissionTemplateConfiguration : IEntityTypeConfiguration<MissionTemplate>
{
    public void Configure(EntityTypeBuilder<MissionTemplate> builder)
    {
        builder.Property(mt => mt.Name)
            .HasMaxLength(200);

        builder.Property(mt => mt.Description)
            .HasMaxLength(1000);

        builder.Property(mt => mt.MissionNamePattern)
            .HasMaxLength(200);

        builder.Property(mt => mt.MissionDescriptionPattern)
            .HasMaxLength(1000);

        builder.HasOne(mt => mt.Organization)
            .WithMany()
            .HasForeignKey(mt => mt.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mt => mt.OrganizationId);

        builder.HasMany(mt => mt.Metrics)
            .WithOne(mtm => mtm.MissionTemplate)
            .HasForeignKey(mtm => mtm.MissionTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mt => mt.Objectives)
            .WithOne(mto => mto.MissionTemplate)
            .HasForeignKey(mto => mto.MissionTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
