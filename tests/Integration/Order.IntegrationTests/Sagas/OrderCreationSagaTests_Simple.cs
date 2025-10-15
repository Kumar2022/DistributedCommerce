using BuildingBlocks.Saga.Abstractions;
using BuildingBlocks.Saga.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Sagas;
using Xunit;

namespace Order.IntegrationTests.Sagas;

/// <summary>
/// Simplified integration tests for Order Creation Saga
/// These are basic smoke tests to verify saga infrastructure is working
/// Full E2E tests with Testcontainers will be added in Phase 2
/// </summary>
[Collection("SagaTests")]
[Trait("Category", "Integration")]
[Trait("Category", "Saga")]
public class OrderCreationSagaTests_Simple : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderCreationSagaTests_Simple(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "Saga Repository: Can Save and Retrieve Saga State")]
    public async Task SagaRepository_ShouldSaveAndRetrieveSagaState()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var sagaRepository = scope.ServiceProvider
            .GetRequiredService<ISagaStateRepository<OrderCreationSagaState>>();

        var sagaState = new OrderCreationSagaState
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 100.00m,
            Currency = "USD"
        };

        sagaState.MarkAsStarted();

        // Act
        await sagaRepository.SaveAsync(sagaState);
        var retrieved = await sagaRepository.GetByCorrelationIdAsync(sagaState.CorrelationId);

        // Assert
        retrieved.Should().NotBeNull("saga state should be retrieved");
        retrieved!.OrderId.Should().Be(sagaState.OrderId);
        retrieved.Status.Should().Be(SagaStatus.InProgress);
        retrieved.TotalAmount.Should().Be(100.00m);
    }

    [Fact(DisplayName = "Saga State: Can Track Step Completion")]
    public async Task SagaState_ShouldTrackStepCompletion()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var sagaRepository = scope.ServiceProvider
            .GetRequiredService<ISagaStateRepository<OrderCreationSagaState>>();

        var sagaState = new OrderCreationSagaState
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 100.00m
        };

        // Act
        sagaState.MarkAsStarted();
        sagaState.AddCompletedStep("ReserveInventory");
        sagaState.AddCompletedStep("ProcessPayment");
        sagaState.MarkAsCompleted();

        await sagaRepository.SaveAsync(sagaState);
        var retrieved = await sagaRepository.GetByCorrelationIdAsync(sagaState.CorrelationId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(SagaStatus.Completed);
        retrieved.CompletedSteps.Should().HaveCount(2);
        retrieved.CompletedSteps.Should().Contain(step => step.Contains("ReserveInventory"));
        retrieved.CompletedSteps.Should().Contain(step => step.Contains("ProcessPayment"));
        retrieved.CurrentStep.Should().Be(2);
    }

    [Fact(DisplayName = "Saga State: Can Track Compensation")]
    public async Task SagaState_ShouldTrackCompensation()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var sagaRepository = scope.ServiceProvider
            .GetRequiredService<ISagaStateRepository<OrderCreationSagaState>>();

        var sagaState = new OrderCreationSagaState
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 100.00m
        };

        // Act - Simulate failure and compensation
        sagaState.MarkAsStarted();
        sagaState.AddCompletedStep("ReserveInventory");
        sagaState.MarkAsCompensating();
        sagaState.AddCompensatedStep("ReleaseInventory");
        sagaState.MarkAsCompensated();

        await sagaRepository.SaveAsync(sagaState);
        var retrieved = await sagaRepository.GetByCorrelationIdAsync(sagaState.CorrelationId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(SagaStatus.Compensated);
        retrieved.CompletedSteps.Should().HaveCount(1);
        retrieved.CompensatedSteps.Should().HaveCount(1);
        retrieved.CompensatedSteps.Should().Contain(step => step.Contains("ReleaseInventory"));
    }

    [Fact(DisplayName = "Saga State: Can Track Failures")]
    public async Task SagaState_ShouldTrackFailures()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var sagaRepository = scope.ServiceProvider
            .GetRequiredService<ISagaStateRepository<OrderCreationSagaState>>();

        var sagaState = new OrderCreationSagaState
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 100.00m
        };

        // Act - Simulate failure
        sagaState.MarkAsStarted();
        sagaState.MarkAsFailed("Payment gateway timeout", "Stack trace here");

        await sagaRepository.SaveAsync(sagaState);
        var retrieved = await sagaRepository.GetByCorrelationIdAsync(sagaState.CorrelationId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(SagaStatus.Failed);
        retrieved.ErrorMessage.Should().Contain("Payment gateway timeout");
        retrieved.ErrorStackTrace.Should().Contain("Stack trace here");
        retrieved.CompletedAt.Should().NotBeNull();
    }
}
