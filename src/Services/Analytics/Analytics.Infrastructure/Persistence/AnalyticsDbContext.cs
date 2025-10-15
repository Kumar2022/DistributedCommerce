using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Persistence;

/// <summary>
/// Analytics database context with Inbox and DLQ pattern support
/// Note: Analytics service is consumer-only, no Outbox needed
/// </summary>
public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : BaseDbContext(options)
{
    // Domain Entities
    public DbSet<OrderMetrics> OrderMetrics => Set<OrderMetrics>();
    public DbSet<ProductMetrics> ProductMetrics => Set<ProductMetrics>();
    public DbSet<CustomerMetrics> CustomerMetrics => Set<CustomerMetrics>();
    public DbSet<RevenueMetrics> RevenueMetrics => Set<RevenueMetrics>();

    // Inbox Pattern - For consuming integration events idempotently (CRITICAL for Analytics service)
    public DbSet<InboxMessage>? InboxMessages { get; set; }
    
    // Dead Letter Queue - For failed message handling
    public DbSet<DeadLetterMessage>? DeadLetterMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnalyticsDbContext).Assembly);

        // Set schema
        modelBuilder.HasDefaultSchema("analytics");
    }
}
