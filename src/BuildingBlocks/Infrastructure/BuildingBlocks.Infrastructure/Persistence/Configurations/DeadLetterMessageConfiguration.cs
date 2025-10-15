using BuildingBlocks.Infrastructure.DeadLetterQueue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for DeadLetterMessage entity
/// </summary>
public sealed class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.ToTable("dead_letter_messages");

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

        builder.Property(x => x.OriginalTimestamp)
            .HasColumnName("original_timestamp")
            .IsRequired();

        builder.Property(x => x.MovedToDlqAt)
            .HasColumnName("moved_to_dlq_at")
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.ErrorDetails)
            .HasColumnName("error_details")
            .HasColumnType("text");

        builder.Property(x => x.TotalAttempts)
            .HasColumnName("total_attempts")
            .IsRequired();

        builder.Property(x => x.ServiceName)
            .HasColumnName("service_name")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id");
        
        builder.Property(x => x.OriginalMessageId)
            .HasColumnName("original_message_id");

        builder.Property(x => x.Reprocessed)
            .HasColumnName("reprocessed")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.ReprocessedAt)
            .HasColumnName("reprocessed_at");

        builder.Property(x => x.OperatorNotes)
            .HasColumnName("operator_notes")
            .HasColumnType("text");

        // Indexes for performance and querying
        builder.HasIndex(x => x.ServiceName)
            .HasDatabaseName("idx_dlq_service_name");

        builder.HasIndex(x => x.EventType)
            .HasDatabaseName("idx_dlq_event_type");

        builder.HasIndex(x => x.MovedToDlqAt)
            .HasDatabaseName("idx_dlq_moved_at");

        builder.HasIndex(x => x.Reprocessed)
            .HasDatabaseName("idx_dlq_reprocessed");
        
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("idx_dlq_correlation_id");

        builder.HasIndex(x => new { x.ServiceName, x.Reprocessed, x.MovedToDlqAt })
            .HasDatabaseName("idx_dlq_service_status_date");
    }
}
