using Order.Application.Commands;
using Order.Application.DTOs;
using Order.Application.Sagas;
using Order.Application.Sagas.Steps;
using BuildingBlocks.Saga.Storage;
using Microsoft.Extensions.Logging;
using OrderDomain = Order.Domain.Aggregates.OrderAggregate;

namespace Order.UnitTests.Application.Commands;

/// <summary>
/// Unit tests for CreateOrderCommandHandler
/// Tests the application layer logic for creating orders
/// NOTE: Saga execution is fire-and-forget, so we focus on testing order creation logic
/// </summary>
public sealed class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly OrderCreationSaga _saga;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        
        // Create saga with mocked dependencies
        // The saga is executed fire-and-forget, so we don't test its execution in command handler tests
        var mockStep1 = new Mock<ReserveInventoryStep>(
            Mock.Of<BuildingBlocks.EventBus.Abstractions.IEventBus>(),
            Mock.Of<ILogger<ReserveInventoryStep>>());
        var mockStep2 = new Mock<ProcessPaymentStep>(
            Mock.Of<BuildingBlocks.EventBus.Abstractions.IEventBus>(),
            Mock.Of<ILogger<ProcessPaymentStep>>());
        var mockStep3 = new Mock<ConfirmOrderStep>(
            Mock.Of<BuildingBlocks.EventBus.Abstractions.IEventBus>(),
            Mock.Of<ILogger<ConfirmOrderStep>>());
        var mockSagaRepo = new Mock<ISagaStateRepository<OrderCreationSagaState>>();
        var mockLogger = new Mock<ILogger<OrderCreationSaga>>();
        var mockOrchestratorLogger = new Mock<ILogger<BuildingBlocks.Saga.Orchestration.SagaOrchestrator<OrderCreationSagaState>>>();
        
        _saga = new OrderCreationSaga(
            mockStep1.Object,
            mockStep2.Object,
            mockStep3.Object,
            mockSagaRepo.Object,
            mockLogger.Object,
            mockOrchestratorLogger.Object);
        
        _handler = new CreateOrderCommandHandler(
            _mockOrderRepository.Object,
            _saga);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateOrder()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Test Product",
                    Quantity: 2,
                    UnitPrice: 29.99m,
                    Currency: "USD")
            });

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        
        _mockOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleItems_ShouldCreateOrderWithAllItems()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "456 Oak Ave",
            City: "Seattle",
            State: "WA",
            PostalCode: "98101",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Product 1", 1, 10.00m, "USD"),
                new(Guid.NewGuid(), "Product 2", 2, 20.00m, "USD"),
                new(Guid.NewGuid(), "Product 3", 3, 30.00m, "USD")
            });

        OrderDomain.Order? capturedOrder = null;
        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()))
            .Callback<OrderDomain.Order, CancellationToken>((order, _) => capturedOrder = order)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Items.Should().HaveCount(3);
        capturedOrder.TotalAmount.Amount.Should().Be(140.00m); // 10 + 40 + 90
    }

    [Fact]
    public async Task Handle_InvalidStreet_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "", // Invalid: empty street
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        
        _mockOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidCity_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "", // Invalid: empty city
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        
        _mockOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidPostalCode_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "", // Invalid: empty postal code
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        
        _mockOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidCountry_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "", // Invalid: empty country
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        
        _mockOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NegativePrice_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, -10.00m, "USD") // Invalid: negative price
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        
        _mockOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ZeroQuantity_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 0, 29.99m, "USD") // Invalid: zero quantity
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        
        _mockOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidCurrencyCode_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "US") // Invalid: 2 chars instead of 3
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        
        _mockOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_VerifiesOrderIsAddedToRepository()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(
            CustomerId: customerId,
            Street: "789 Elm St",
            City: "Portland",
            State: "OR",
            PostalCode: "97201",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Premium Widget", 5, 15.50m, "USD")
            });

        OrderDomain.Order? savedOrder = null;
        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<OrderDomain.Order>(), It.IsAny<CancellationToken>()))
            .Callback<OrderDomain.Order, CancellationToken>((order, _) => savedOrder = order)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        savedOrder.Should().NotBeNull();
        savedOrder!.CustomerId.Value.Should().Be(customerId);
        savedOrder.ShippingAddress.Street.Should().Be("789 Elm St");
        savedOrder.Items.Should().HaveCount(1);
    }
}
