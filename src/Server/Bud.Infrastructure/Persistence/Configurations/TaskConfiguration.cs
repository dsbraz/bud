using Bud.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Infrastructure.Persistence.Configurations;

public sealed class TaskConfiguration : IEntityTypeConfiguration<GoalTask>
{
    public void Configure(EntityTypeBuilder<GoalTask> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.HasOne(t => t.Organization)
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.OrganizationId);

        builder.HasOne(t => t.Goal)
            .WithMany(g => g.Tasks)
            .HasForeignKey(t => t.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.GoalId);
        builder.HasIndex(t => new { t.GoalId, t.State });
    }
}
