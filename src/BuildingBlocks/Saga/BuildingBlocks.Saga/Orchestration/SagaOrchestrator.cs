using BuildingBlocks.Saga.Abstractions;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Saga.Orchestration;

/// <summary>
/// Orchestrates saga execution with automatic compensation on failure
/// </summary>
public class SagaOrchestrator<TState>(ILogger<SagaOrchestrator<TState>> logger)
    where TState : SagaState
{
    private readonly List<ISagaStep<TState>> _steps = [];

    /// <summary>
    /// Add a step to the saga
    /// </summary>
    public SagaOrchestrator<TState> AddStep(ISagaStep<TState> step)
    {
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Execute all saga steps in order
    /// </summary>
    public async Task<bool> ExecuteAsync(TState state, CancellationToken cancellationToken = default)
    {
        state.MarkAsStarted();
        logger.LogInformation("Starting saga execution. CorrelationId: {CorrelationId}, Steps: {StepCount}",
            state.CorrelationId, _steps.Count);

        var executedSteps = new List<ISagaStep<TState>>();

        try
        {
            foreach (var step in _steps)
            {
                logger.LogInformation("Executing step: {StepName} (Correlation: {CorrelationId})", 
                    step.StepName, state.CorrelationId);

                var result = await step.ExecuteAsync(state, cancellationToken);

                if (!result.IsSuccess)
                {
                    logger.LogError("Step {StepName} failed: {ErrorMessage}. Starting compensation... (Correlation: {CorrelationId})",
                        step.StepName, result.ErrorMessage, state.CorrelationId);

                    state.MarkAsFailed(
                        result.ErrorMessage ?? "Unknown error", 
                        result.Exception?.StackTrace);

                    // Compensate all executed steps in reverse order
                    await CompensateAsync(executedSteps, state, cancellationToken);
                    return false;
                }

                executedSteps.Add(step);
                state.AddCompletedStep(step.StepName);

                logger.LogInformation("Step {StepName} completed successfully (Correlation: {CorrelationId})",
                    step.StepName, state.CorrelationId);
            }

            state.MarkAsCompleted();
            logger.LogInformation("Saga completed successfully. CorrelationId: {CorrelationId}", 
                state.CorrelationId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Saga execution failed with exception. CorrelationId: {CorrelationId}", 
                state.CorrelationId);

            state.MarkAsFailed(ex.Message, ex.StackTrace);

            // Compensate all executed steps in reverse order
            await CompensateAsync(executedSteps, state, cancellationToken);
            return false;
        }
    }

    /// <summary>
    /// Compensate saga by rolling back all executed steps in reverse order
    /// </summary>
    private async Task CompensateAsync(
        List<ISagaStep<TState>> executedSteps, 
        TState state,
        CancellationToken cancellationToken)
    {
        state.MarkAsCompensating();
        logger.LogInformation("Starting saga compensation. CorrelationId: {CorrelationId}, Steps to compensate: {StepCount}",
            state.CorrelationId, executedSteps.Count);

        // Reverse the order for compensation
        executedSteps.Reverse();

        foreach (var step in executedSteps)
        {
            try
            {
                logger.LogInformation("Compensating step: {StepName} (Correlation: {CorrelationId})",
                    step.StepName, state.CorrelationId);

                await step.CompensateAsync(state, cancellationToken);
                state.AddCompensatedStep(step.StepName);

                logger.LogInformation("Step {StepName} compensated successfully (Correlation: {CorrelationId})",
                    step.StepName, state.CorrelationId);
            }
            catch (Exception ex)
            {
                // Log compensation failure but continue with other compensations
                logger.LogError(ex, "Failed to compensate step {StepName}. Continuing with other compensations... (Correlation: {CorrelationId})",
                    step.StepName, state.CorrelationId);
            }
        }

        state.MarkAsCompensated();
        logger.LogInformation("Saga compensation completed. CorrelationId: {CorrelationId}", 
            state.CorrelationId);
    }
}
