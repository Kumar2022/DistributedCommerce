using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Outbox;
using BuildingBlocks.Saga.Storage;
using Microsoft.EntityFrameworkCore;

namespace Order.Infrastructure.Persistence;

/// <summary>
/// DbContext for Order service infrastructure concerns (Outbox, Inbox, DLQ, Saga state).
/// Note: Order aggregate uses Marten (event sourcing), not EF Core.
/// This context is ONLY for cross-cutting infrastructure patterns.
/// </summary>
public sealed class OrderInfrastructureDbContext : DbContext
{
    public OrderInfrastructureDbContext(DbContextOptions<OrderInfrastructureDbContext> options)
        : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();
    public DbSet<SagaStateEntity> SagaStates => Set<SagaStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply BuildingBlocks configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(OutboxMessage).Assembly); // BuildingBlocks.Infrastructure

        // Schema prefix for this service
        modelBuilder.Entity<OutboxMessage>().ToTable("OutboxMessages", "order_infra");
        modelBuilder.Entity<InboxMessage>().ToTable("InboxMessages", "order_infra");
        modelBuilder.Entity<DeadLetterMessage>().ToTable("DeadLetterMessages", "order_infra");
        modelBuilder.Entity<SagaStateEntity>().ToTable("SagaStates", "order_infra");
    }
}
