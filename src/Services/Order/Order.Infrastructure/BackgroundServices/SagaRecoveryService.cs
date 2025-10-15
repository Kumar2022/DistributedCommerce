using BuildingBlocks.Saga.Abstractions;
using BuildingBlocks.Saga.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Application.Sagas;

namespace Order.Infrastructure.BackgroundServices;

/// <summary>
/// Background service to recover timed-out or stuck sagas
/// FAANG-scale reliability: ensures saga completion despite process restarts
/// </summary>
public class SagaRecoveryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaRecoveryService> _logger;
    private readonly TimeSpan _scanInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _sagaTimeout = TimeSpan.FromMinutes(30);

    public SagaRecoveryService(
        IServiceProvider serviceProvider,
        ILogger<SagaRecoveryService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Saga Recovery Service started. Scan interval: {Interval}, Timeout: {Timeout}",
            _scanInterval, _sagaTimeout);

        // Wait a bit before first scan to allow services to start up
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecoverTimedOutSagasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during saga recovery scan");
            }

            await Task.Delay(_scanInterval, stoppingToken);
        }

        _logger.LogInformation("Saga Recovery Service stopped");
    }

    private async Task RecoverTimedOutSagasAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISagaStateRepository<OrderCreationSagaState>>();
        var saga = scope.ServiceProvider.GetRequiredService<OrderCreationSaga>();

        try
        {
            // Find sagas that are stuck in InProgress state past timeout
            var timedOutSagas = await repository.GetTimedOutSagasAsync(_sagaTimeout, cancellationToken);

            if (timedOutSagas.Count == 0)
            {
                _logger.LogDebug("No timed-out sagas found");
                return;
            }

            _logger.LogWarning("Found {Count} timed-out sagas. Starting recovery...", timedOutSagas.Count);

            foreach (var sagaState in timedOutSagas)
            {
                try
                {
                    _logger.LogInformation(
                        "Recovering saga {CorrelationId} for Order {OrderId}, Current Step: {Step}, Status: {Status}",
                        sagaState.CorrelationId,
                        sagaState.OrderId,
                        sagaState.CurrentStep,
                        sagaState.Status);

                    // Strategy: Attempt compensation for timed-out sagas
                    // In production, you might want more sophisticated logic:
                    // - Check if partial steps succeeded and can be retried
                    // - Implement idempotent resume logic
                    // - Alert operators for manual intervention on critical flows
                    await saga.CompensateAsync(sagaState, cancellationToken);

                    _logger.LogInformation(
                        "Successfully compensated timed-out saga {CorrelationId}",
                        sagaState.CorrelationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to recover saga {CorrelationId} for Order {OrderId}",
                        sagaState.CorrelationId,
                        sagaState.OrderId);
                    
                    // Continue with other sagas even if one fails
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving timed-out sagas");
        }
    }
}
