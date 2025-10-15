using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base DbContext with conventions for domain entities
/// Automatically publishes domain events to outbox and handles timestamps
/// Implements Transactional Outbox Pattern for reliable event publishing
/// </summary>
public abstract class BaseDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Override to include OutboxMessages in your DbContext if needed
    /// </summary>
    public virtual DbSet<OutboxMessage>? OutboxMessages { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events before saving
        var domainEvents = GetDomainEvents();
        
        // Update timestamps
        UpdateTimestamps();
        
        // Save changes (this persists entities AND outbox messages in same transaction)
        var result = await base.SaveChangesAsync(cancellationToken);
        
        // Convert domain events to outbox messages
        if (OutboxMessages == null || domainEvents.Count == 0) return result;
        await AddDomainEventsToOutboxAsync(domainEvents, cancellationToken);
        await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            return;
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            await (_currentTransaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await (_currentTransaction?.RollbackAsync(cancellationToken) ?? Task.CompletedTask);
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the same assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        
        // Apply shared configurations from BuildingBlocks
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseDbContext).Assembly);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            // Handle entities with CreatedAt/UpdatedAt properties
            var createdAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
            var updatedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");

            if (entry.State == EntityState.Added && createdAtProperty != null)
            {
                createdAtProperty.CurrentValue = DateTime.UtcNow;
            }

            if (updatedAtProperty != null)
            {
                updatedAtProperty.CurrentValue = DateTime.UtcNow;
            }
        }
    }

    private List<IDomainEvent> GetDomainEvents()
    {
        var domainEvents = new List<IDomainEvent>();
        
        // Get all entities that have domain events
        var entities = ChangeTracker.Entries()
            .Where(e => e.Entity.GetType().GetProperty("DomainEvents") != null)
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entities)
        {
            var domainEventsProperty = entity.GetType().GetProperty("DomainEvents");
            if (domainEventsProperty == null) continue;
            
            var events = domainEventsProperty.GetValue(entity) as IEnumerable<IDomainEvent>;
            if (events == null || !events.Any()) continue;
            
            domainEvents.AddRange(events);
            
            // Clear domain events
            var clearMethod = entity.GetType().GetMethod("ClearDomainEvents");
            clearMethod?.Invoke(entity, null);
        }

        return domainEvents;
    }

    private async Task AddDomainEventsToOutboxAsync(
        List<IDomainEvent> domainEvents, 
        CancellationToken cancellationToken)
    {
        if (OutboxMessages == null) return;

        foreach (var outboxMessage in domainEvents.Select(domainEvent => new OutboxMessage
                 {
                     Id = Guid.NewGuid(),
                     EventType = domainEvent.GetType().Name,
                     Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                     OccurredAt = domainEvent.OccurredAt,
                     CorrelationId = domainEvent.CorrelationId
                 }))
        {
            await OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        }
    }
}

