using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Persistence;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Inventory database context with Outbox, Inbox, and DLQ pattern support
/// </summary>
public class InventoryDbContext : BaseDbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) 
        : base(options)
    {
    }

    // Domain Entities
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    // Inbox Pattern - For consuming integration events idempotently
    public DbSet<InboxMessage>? InboxMessages { get; set; }
    
    // Dead Letter Queue - For failed message handling
    public DbSet<DeadLetterMessage>? DeadLetterMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("inventory");

        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new StockReservationConfiguration());
    }
}
