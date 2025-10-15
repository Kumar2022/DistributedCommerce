using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Persistence;
using Identity.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Identity database context with Outbox and DLQ pattern support
/// Note: Identity service produces user events, doesn't need Inbox
/// </summary>
public sealed class IdentityDbContext : BaseDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) 
        : base(options)
    {
    }

    // Domain Entities
    public DbSet<User> Users => Set<User>();

    // Dead Letter Queue - For failed message handling
    public DbSet<DeadLetterMessage>? DeadLetterMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        
        // Set default schema
        modelBuilder.HasDefaultSchema("identity");
    }
}
