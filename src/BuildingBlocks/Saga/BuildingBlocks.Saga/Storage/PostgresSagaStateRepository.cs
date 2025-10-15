using System.Text.Json;
using BuildingBlocks.Saga.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Saga.Storage;

/// <summary>
/// PostgreSQL-based saga state repository for durable saga state persistence
/// Critical for FAANG-scale systems to survive pod restarts and ensure saga recovery
/// </summary>
public class PostgresSagaStateRepository<TState>(
    SagaStateDbContext dbContext,
    ILogger<PostgresSagaStateRepository<TState>> logger)
    : ISagaStateRepository<TState>
    where TState : SagaState
{
    private readonly SagaStateDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ILogger<PostgresSagaStateRepository<TState>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TState?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.SagaStates
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            return entity == null ? null : Deserialize(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga state for ID {SagaId}", id);
            throw;
        }
    }

    public async Task<TState?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.SagaStates
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId, cancellationToken);

            return entity == null ? null : Deserialize(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga state for correlation ID {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task SaveAsync(TState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        try
        {
            var stateJson = JsonSerializer.Serialize(state);
            var entity = new SagaStateEntity
            {
                Id = Guid.NewGuid(),
                CorrelationId = state.CorrelationId,
                SagaType = typeof(TState).Name,
                Status = state.Status.ToString(),
                CurrentStep = state.CurrentStep,
                StateJson = stateJson,
                CreatedAt = state.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };

            await _dbContext.SagaStates.AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Saved new saga state for correlation ID {CorrelationId}, Type: {SagaType}",
                state.CorrelationId,
                entity.SagaType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving saga state for correlation ID {CorrelationId}", state.CorrelationId);
            throw;
        }
    }

    public async Task UpdateAsync(TState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        try
        {
            var entity = await _dbContext.SagaStates
                .FirstOrDefaultAsync(s => s.CorrelationId == state.CorrelationId, cancellationToken);

            if (entity == null)
                throw new InvalidOperationException($"Saga state not found for correlation ID {state.CorrelationId}");

            entity.Status = state.Status.ToString();
            entity.CurrentStep = state.CurrentStep;
            entity.StateJson = JsonSerializer.Serialize(state);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Version++;

            _dbContext.SagaStates.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated saga state for correlation ID {CorrelationId}, Status: {Status}, Step: {Step}, Version: {Version}",
                state.CorrelationId,
                entity.Status,
                entity.CurrentStep,
                entity.Version);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict updating saga state for correlation ID {CorrelationId}", state.CorrelationId);
            throw new InvalidOperationException($"Saga state for {state.CorrelationId} was modified by another process", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating saga state for correlation ID {CorrelationId}", state.CorrelationId);
            throw;
        }
    }

    public async Task DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.SagaStates
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId, cancellationToken);

            if (entity != null)
            {
                _dbContext.SagaStates.Remove(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Deleted saga state for correlation ID {CorrelationId}", correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saga state for correlation ID {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<List<TState>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var statusStr = status.ToString();
            var entities = await _dbContext.SagaStates
                .AsNoTracking()
                .Where(s => s.Status == statusStr && s.SagaType == typeof(TState).Name)
                .ToListAsync(cancellationToken);

            return entities.Select(Deserialize).Where(s => s != null).Cast<TState>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sagas by status {Status}", status);
            throw;
        }
    }

    public async Task<List<TState>> GetTimedOutSagasAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - timeout;
            var entities = await _dbContext.SagaStates
                .AsNoTracking()
                .Where(s => s.SagaType == typeof(TState).Name
                    && s.Status == nameof(SagaStatus.InProgress)
                    && s.UpdatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} timed-out sagas (timeout: {Timeout})", entities.Count, timeout);

            return entities.Select(Deserialize).Where(s => s != null).Cast<TState>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving timed-out sagas");
            throw;
        }
    }

    private TState? Deserialize(SagaStateEntity entity)
    {
        try
        {
            return JsonSerializer.Deserialize<TState>(entity.StateJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing saga state for correlation ID {CorrelationId}", entity.CorrelationId);
            return null;
        }
    }
}

/// <summary>
/// EF Core entity for saga state persistence
/// </summary>
public class SagaStateEntity
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public string StateJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; }
}

/// <summary>
/// EF Core DbContext for saga state storage
/// </summary>
public class SagaStateDbContext(DbContextOptions<SagaStateDbContext> options) : DbContext(options)
{
    public DbSet<SagaStateEntity> SagaStates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SagaStateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.CorrelationId)
                .IsUnique();

            entity.Property(e => e.SagaType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.StateJson)
                .IsRequired()
                .HasColumnType("jsonb"); // PostgreSQL JSONB for efficient querying

            entity.Property(e => e.Version)
                .IsConcurrencyToken(); // Optimistic concurrency

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => new { e.Status, e.UpdatedAt }); // Composite index for cleanup queries
            entity.HasIndex(e => new { e.SagaType, e.Status }); // For type-specific queries

            entity.ToTable("saga_states");
        });
    }
}
