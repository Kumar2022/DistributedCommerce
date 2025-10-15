using BuildingBlocks.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for InboxMessage entity
/// FAANG-scale pattern: Unique constraint on (EventId, Consumer) for proper idempotency
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Consumer)
            .HasColumnName("consumer")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("received_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.Error)
            .HasColumnName("error")
            .HasMaxLength(2000);

        builder.Property(x => x.ProcessingAttempts)
            .HasColumnName("processing_attempts")
            .HasDefaultValue(0)
            .IsRequired();
        
        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id");

        // CRITICAL: Unique constraint on (EventId, Consumer) for idempotency
        // Allows same event to be processed by different consumers
        builder.HasIndex(x => new { x.EventId, x.Consumer })
            .IsUnique()
            .HasDatabaseName("idx_inbox_event_consumer_unique");

        // Indexes for performance
        builder.HasIndex(x => x.ProcessedAt)
            .HasDatabaseName("idx_inbox_processed_at");

        builder.HasIndex(x => new { x.ProcessedAt, x.ProcessingAttempts })
            .HasDatabaseName("idx_inbox_unprocessed");

        builder.HasIndex(x => x.ReceivedAt)
            .HasDatabaseName("idx_inbox_received_at");
        
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("idx_inbox_correlation_id");
    }
}
