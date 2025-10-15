using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Notification.Domain.Aggregates;
using NotificationAggregate = Notification.Domain.Aggregates.Notification;

namespace Notification.Infrastructure.Persistence;

/// <summary>
/// Notification database context with Inbox and DLQ pattern support
/// Note: Notification service is consumer-only, no Outbox needed
/// </summary>
public sealed class NotificationDbContext : BaseDbContext, BuildingBlocks.Domain.IUnitOfWork
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    // Domain Entities
    public DbSet<NotificationAggregate> Notifications => Set<NotificationAggregate>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    // Inbox Pattern - For consuming integration events idempotently (CRITICAL for Notification service)
    public DbSet<InboxMessage>? InboxMessages { get; set; }
    
    // Dead Letter Queue - For failed message handling
    public DbSet<DeadLetterMessage>? DeadLetterMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Set default schema
        modelBuilder.HasDefaultSchema("notification");
    }
}
