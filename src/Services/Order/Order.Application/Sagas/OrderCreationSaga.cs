using BuildingBlocks.Saga.Abstractions;
using BuildingBlocks.Saga.Orchestration;
using BuildingBlocks.Saga.Storage;
using Microsoft.Extensions.Logging;
using Order.Application.Sagas.Steps;

namespace Order.Application.Sagas;

/// <summary>
/// Order Creation Saga Orchestrator
/// Coordinates: Inventory Reservation → Payment Processing → Order Confirmation
/// </summary>
public class OrderCreationSaga : ISaga<OrderCreationSagaState>
{
    private readonly SagaOrchestrator<OrderCreationSagaState> _orchestrator;
    private readonly ISagaStateRepository<OrderCreationSagaState> _stateRepository;
    private readonly ILogger<OrderCreationSaga> _logger;

    public string SagaName => "OrderCreationSaga";

    public OrderCreationSaga(
        ReserveInventoryStep reserveInventoryStep,
        ProcessPaymentStep processPaymentStep,
        ConfirmOrderStep confirmOrderStep,
        ISagaStateRepository<OrderCreationSagaState> stateRepository,
        ILogger<OrderCreationSaga> logger,
        ILogger<SagaOrchestrator<OrderCreationSagaState>> orchestratorLogger)
    {
        _stateRepository = stateRepository;
        _logger = logger;
        
        // Build the saga orchestrator with steps in order
        _orchestrator = new SagaOrchestrator<OrderCreationSagaState>(orchestratorLogger)
            .AddStep(reserveInventoryStep)
            .AddStep(processPaymentStep)
            .AddStep(confirmOrderStep);
    }

    public async Task ExecuteAsync(
        OrderCreationSagaState state, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting {SagaName} for Order {OrderId}",
            SagaName, state.OrderId);

        try
        {
            // Save initial state
            await _stateRepository.SaveAsync(state, cancellationToken);

            // Execute the saga
            var success = await _orchestrator.ExecuteAsync(state, cancellationToken);

            // Save final state
            await _stateRepository.UpdateAsync(state, cancellationToken);

            if (success)
            {
                _logger.LogInformation(
                    "{SagaName} completed successfully for Order {OrderId}. Status: {Status}",
                    SagaName, state.OrderId, state.Status);
            }
            else
            {
                _logger.LogWarning(
                    "{SagaName} failed for Order {OrderId}. Status: {Status}, Error: {Error}",
                    SagaName, state.OrderId, state.Status, state.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{SagaName} execution failed for Order {OrderId}",
                SagaName, state.OrderId);

            // Save error state
            state.MarkAsFailed(ex.Message, ex.StackTrace);
            await _stateRepository.UpdateAsync(state, cancellationToken);

            throw;
        }
    }

    public async Task CompensateAsync(
        OrderCreationSagaState state, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Manually compensating {SagaName} for Order {OrderId}",
            SagaName, state.OrderId);

        // Manual compensation if needed
        // The orchestrator handles automatic compensation on failure
        state.MarkAsCompensating();
        await _stateRepository.UpdateAsync(state, cancellationToken);

        _logger.LogInformation(
            "{SagaName} compensation completed for Order {OrderId}",
            SagaName, state.OrderId);
    }

    /// <summary>
    /// Get saga state by correlation ID
    /// </summary>
    public async Task<OrderCreationSagaState?> GetStateAsync(
        Guid correlationId, 
        CancellationToken cancellationToken = default)
    {
        return await _stateRepository.GetByCorrelationIdAsync(
            correlationId, 
            cancellationToken);
    }

    /// <summary>
    /// Delete saga state (cleanup after completion)
    /// </summary>
    public async Task DeleteStateAsync(
        Guid correlationId, 
        CancellationToken cancellationToken = default)
    {
        await _stateRepository.DeleteAsync(correlationId, cancellationToken);
        
        _logger.LogInformation(
            "Saga state deleted for correlation ID {CorrelationId}",
            correlationId);
    }
}
