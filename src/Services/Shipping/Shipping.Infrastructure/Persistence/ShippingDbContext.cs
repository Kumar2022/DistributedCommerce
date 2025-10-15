using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shipping.Domain.Aggregates;
using Shipping.Infrastructure.Persistence.Configurations;

namespace Shipping.Infrastructure.Persistence;

/// <summary>
/// Shipping database context with Outbox, Inbox, and DLQ pattern support
/// Implements DbContext with domain entity configurations and IUnitOfWork
/// </summary>
public sealed class ShippingDbContext : BaseDbContext, BuildingBlocks.Domain.IUnitOfWork
{
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options)
        : base(options)
    {
    }

    // Domain Entities
    public DbSet<Shipment> Shipments => Set<Shipment>();

    // Inbox Pattern - For consuming integration events idempotently
    public DbSet<InboxMessage>? InboxMessages { get; set; }
    
    // Dead Letter Queue - For failed message handling
    public DbSet<DeadLetterMessage>? DeadLetterMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new ShipmentConfiguration());

        // Set default schema
        modelBuilder.HasDefaultSchema("shipping");
    }
}
