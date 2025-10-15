using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Payment.Infrastructure.Persistence.Configurations;

namespace Payment.Infrastructure.Persistence;

/// <summary>
/// Payment database context with Outbox, Inbox, and DLQ pattern support
/// </summary>
public sealed class PaymentDbContext : BaseDbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    // Domain Entities
    public DbSet<Domain.Aggregates.PaymentAggregate.Payment> Payments => Set<Domain.Aggregates.PaymentAggregate.Payment>();

    // Inbox Pattern - For consuming integration events idempotently
    public DbSet<InboxMessage>? InboxMessages { get; set; }
    
    // Dead Letter Queue - For failed message handling
    public DbSet<DeadLetterMessage>? DeadLetterMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PaymentConfiguration());

        modelBuilder.HasDefaultSchema("payment");
    }
}
