using BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for OutboxMessage entity
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.Error)
            .HasColumnName("error")
            .HasMaxLength(2000);

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0)
            .IsRequired();
        
        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id");
        
        builder.Property(x => x.AggregateId)
            .HasColumnName("aggregate_id");

        // Indexes for performance
        builder.HasIndex(x => x.ProcessedAt)
            .HasDatabaseName("idx_outbox_processed_at");

        builder.HasIndex(x => new { x.ProcessedAt, x.RetryCount })
            .HasDatabaseName("idx_outbox_unprocessed");

        builder.HasIndex(x => x.OccurredAt)
            .HasDatabaseName("idx_outbox_occurred_at");
        
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("idx_outbox_correlation_id");
    }
}
