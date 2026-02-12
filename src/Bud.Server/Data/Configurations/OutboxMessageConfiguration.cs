using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bud.Server.Data.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .HasMaxLength(1000);

        builder.Property(o => o.Payload)
            .HasColumnType("text");

        builder.Property(o => o.Error)
            .HasColumnType("text");

        builder.HasIndex(o => new { o.ProcessedOnUtc, o.DeadLetteredOnUtc, o.NextAttemptOnUtc, o.OccurredOnUtc });
    }
}
