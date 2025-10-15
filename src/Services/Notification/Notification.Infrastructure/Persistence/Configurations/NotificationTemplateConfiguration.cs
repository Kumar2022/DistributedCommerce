using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Aggregates;

namespace Notification.Infrastructure.Persistence.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.Channel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.SubjectTemplate).HasMaxLength(500).IsRequired();
        builder.Property(t => t.BodyTemplate).IsRequired();
        builder.Property(t => t.Category).HasMaxLength(100);
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.DefaultVariables).HasColumnType("jsonb");
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasIndex(t => t.Channel);
        builder.HasIndex(t => t.Category);
        builder.HasIndex(t => t.IsActive);

        builder.Ignore(t => t.DomainEvents);
    }
}
