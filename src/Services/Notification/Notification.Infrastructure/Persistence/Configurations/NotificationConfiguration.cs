using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationAggregate = Notification.Domain.Aggregates.Notification;

namespace Notification.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<NotificationAggregate>
{
    public void Configure(EntityTypeBuilder<NotificationAggregate> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.OwnsOne(n => n.Recipient, recipient =>
        {
            recipient.Property(r => r.UserId).HasColumnName("UserId").IsRequired();
            recipient.Property(r => r.Email).HasColumnName("Email").HasMaxLength(200).IsRequired();
            recipient.Property(r => r.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
            recipient.Property(r => r.PhoneNumber).HasColumnName("PhoneNumber").HasMaxLength(20);
        });

        builder.OwnsOne(n => n.Content, content =>
        {
            content.Property(c => c.Subject).HasColumnName("Subject").HasMaxLength(500).IsRequired();
            content.Property(c => c.Body).HasColumnName("Body").IsRequired();
            content.Property(c => c.Variables).HasColumnName("Variables").HasColumnType("jsonb");
        });

        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(n => n.Priority).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(n => n.TemplateId);
        builder.Property(n => n.ExternalId).HasMaxLength(200);
        builder.Property(n => n.RetryCount).IsRequired();
        builder.Property(n => n.ErrorMessage).HasMaxLength(1000);
        builder.Property(n => n.ScheduledFor);
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.SentAt);
        builder.Property(n => n.DeliveredAt);
        builder.Property(n => n.FailedAt);

        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.Channel);
        builder.HasIndex(n => new { n.Status, n.ScheduledFor });
        
        builder.OwnsOne(n => n.Recipient, recipient =>
        {
            recipient.HasIndex(r => r.UserId).HasDatabaseName("IX_Notifications_UserId");
        });

        builder.Ignore(n => n.DomainEvents);
    }
}
