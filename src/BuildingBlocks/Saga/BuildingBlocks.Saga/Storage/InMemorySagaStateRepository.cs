using System.Collections.Concurrent;
using BuildingBlocks.Saga.Abstractions;

namespace BuildingBlocks.Saga.Storage;

/// <summary>
/// In-memory implementation of saga state repository (for development/testing)
/// </summary>
public class InMemorySagaStateRepository<TState> : ISagaStateRepository<TState> where TState : SagaState
{
    private readonly ConcurrentDictionary<Guid, TState> _store = new();

    public Task SaveAsync(TState state, CancellationToken cancellationToken = default)
    {
        _store[state.CorrelationId] = state;
        return Task.CompletedTask;
    }

    public Task<TState?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(correlationId, out var state);
        return Task.FromResult(state);
    }

    public Task<TState?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return GetByCorrelationIdAsync(id, cancellationToken);
    }

    public Task UpdateAsync(TState state, CancellationToken cancellationToken = default)
    {
        _store[state.CorrelationId] = state;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(correlationId, out _);
        return Task.CompletedTask;
    }

    public Task<List<TState>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken = default)
    {
        var states = _store.Values.Where(s => s.Status == status).ToList();
        return Task.FromResult(states);
    }

    public Task<List<TState>> GetTimedOutSagasAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - timeout;
        var timedOutSagas = _store.Values
            .Where(s => s.Status == SagaStatus.InProgress && s.CreatedAt < cutoffTime)
            .ToList();
        return Task.FromResult(timedOutSagas);
    }
}
