using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Catalog database context with Outbox, Inbox, and DLQ pattern support
/// </summary>
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : BaseDbContext(options)
{
    // Domain Entities
    public DbSet<CatalogProduct> Products => Set<CatalogProduct>();
    public DbSet<Category> Categories => Set<Category>();

    // Inbox Pattern - For consuming integration events idempotently
    public DbSet<InboxMessage>? InboxMessages { get; set; }
    
    // Dead Letter Queue - For failed message handling
    public DbSet<DeadLetterMessage>? DeadLetterMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // Set schema name
        modelBuilder.HasDefaultSchema("catalog");
    }
}
