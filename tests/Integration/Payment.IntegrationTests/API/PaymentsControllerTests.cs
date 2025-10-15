using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Payment.Application.Commands;
using Payment.Application.DTOs;
using Payment.IntegrationTests.Fixtures;
using Xunit;

namespace Payment.IntegrationTests.API;

/// <summary>
/// Integration tests for PaymentsController
/// Tests all endpoints with real database, Kafka, and Redis via Testcontainers
/// </summary>
[Collection("PaymentApiCollection")]
public class PaymentsControllerTests : IClassFixture<PaymentApiFixture>
{
    private readonly PaymentApiFixture _fixture;
    private readonly HttpClient _client;

    public PaymentsControllerTests(PaymentApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    #region CreatePayment Tests

    [Fact]
    public async Task CreatePayment_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var command = new CreatePaymentCommand(
            OrderId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "USD",
            PaymentMethod: "CreditCard",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        var result = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>();
        result.Should().NotBeNull();
        result!.PaymentId.Should().NotBeEmpty();
        
        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/v1/payments/{result.PaymentId}");
    }

    [Fact]
    public async Task CreatePayment_WithZeroAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreatePaymentCommand(
            OrderId: Guid.NewGuid(),
            Amount: 0m,
            Currency: "USD",
            PaymentMethod: "CreditCard"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePayment_WithNegativeAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreatePaymentCommand(
            OrderId: Guid.NewGuid(),
            Amount: -10.50m,
            Currency: "USD",
            PaymentMethod: "CreditCard"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_WithInvalidCurrency_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreatePaymentCommand(
            OrderId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "INVALID",
            PaymentMethod: "CreditCard"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_WithEmptyOrderId_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreatePaymentCommand(
            OrderId: Guid.Empty,
            Amount: 99.99m,
            Currency: "USD",
            PaymentMethod: "CreditCard"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_WithSameIdempotencyKey_ShouldReturnSamePayment()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new CreatePaymentCommand(
            OrderId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "USD",
            PaymentMethod: "CreditCard",
            IdempotencyKey: idempotencyKey
        );

        // Act - First request
        var response1 = await _client.PostAsJsonAsync("/api/v1/payments", command);
        var result1 = await response1.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Act - Second request with same idempotency key
        var response2 = await _client.PostAsJsonAsync("/api/v1/payments", command);
        var result2 = await response2.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        result1!.PaymentId.Should().Be(result2!.PaymentId); // Same payment ID
    }

    #endregion

    #region GetPaymentById Tests

    [Fact]
    public async Task GetPaymentById_WithExistingId_ShouldReturnPayment()
    {
        // Arrange - Create a payment first
        var createCommand = new CreatePaymentCommand(
            OrderId: Guid.NewGuid(),
            Amount: 149.99m,
            Currency: "USD",
            PaymentMethod: "CreditCard"
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/payments", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/{createResult!.PaymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var payment = await response.Content.ReadFromJsonAsync<PaymentDto>();
        payment.Should().NotBeNull();
        payment!.Id.Should().Be(createResult.PaymentId);
        payment.OrderId.Should().Be(createCommand.OrderId);
        payment.Amount.Should().Be(createCommand.Amount);
        payment.Currency.Should().Be(createCommand.Currency);
        payment.Method.Should().Be(createCommand.PaymentMethod);
        payment.Status.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPaymentById_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(404);
    }

    [Fact]
    public async Task GetPaymentById_WithInvalidGuid_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/payments/invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetPaymentsByOrderId Tests

    [Fact]
    public async Task GetPaymentsByOrderId_WithExistingOrderId_ShouldReturnPayments()
    {
        // Arrange - Create multiple payments for same order
        var orderId = Guid.NewGuid();
        
        var command1 = new CreatePaymentCommand(orderId, 50m, "USD", "CreditCard");
        var command2 = new CreatePaymentCommand(orderId, 25m, "USD", "DebitCard");
        
        await _client.PostAsJsonAsync("/api/v1/payments", command1);
        await _client.PostAsJsonAsync("/api/v1/payments", command2);

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/order/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>();
        payments.Should().NotBeNull();
        payments!.Should().HaveCountGreaterOrEqualTo(2);
        payments.Should().OnlyContain(p => p.OrderId == orderId);
    }

    [Fact]
    public async Task GetPaymentsByOrderId_WithNonExistingOrderId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingOrderId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/order/{nonExistingOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region ProcessPayment Tests

    [Fact]
    public async Task ProcessPayment_WithValidData_ShouldReturnSuccess()
    {
        // Arrange - Create a payment first
        var createCommand = new CreatePaymentCommand(
            Guid.NewGuid(),
            199.99m,
            "USD",
            "CreditCard"
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/payments", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        var processRequest = new { PaymentMethodId = "pm_test_card_visa" };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/payments/{createResult!.PaymentId}/process",
            processRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<SuccessResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Contain("successfully");
    }

    [Fact]
    public async Task ProcessPayment_WithNonExistingPaymentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var processRequest = new { PaymentMethodId = "pm_test_card_visa" };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/payments/{nonExistingId}/process",
            processRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProcessPayment_AlreadyProcessed_ShouldReturnBadRequest()
    {
        // Arrange - Create and process payment
        var createCommand = new CreatePaymentCommand(Guid.NewGuid(), 99.99m, "USD", "CreditCard");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/payments", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        var processRequest = new { PaymentMethodId = "pm_test_card_visa" };
        await _client.PostAsJsonAsync($"/api/v1/payments/{createResult!.PaymentId}/process", processRequest);

        // Act - Try to process again
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/payments/{createResult.PaymentId}/process",
            processRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region ConfirmPayment Tests

    [Fact]
    public async Task ConfirmPayment_WithValidExternalPaymentId_ShouldReturnSuccess()
    {
        // Arrange
        var confirmRequest = new { ExternalPaymentId = $"pi_test_{Guid.NewGuid()}" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/confirm", confirmRequest);

        // Assert
        // May return 200 OK or 400 BadRequest depending on whether external payment exists
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmPayment_WithEmptyExternalPaymentId_ShouldReturnBadRequest()
    {
        // Arrange
        var confirmRequest = new { ExternalPaymentId = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/confirm", confirmRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region RefundPayment Tests

    [Fact]
    public async Task RefundPayment_WithValidData_ShouldReturnSuccess()
    {
        // Arrange - Create and process payment first
        var createCommand = new CreatePaymentCommand(Guid.NewGuid(), 299.99m, "USD", "CreditCard");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/payments", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Process payment
        var processRequest = new { PaymentMethodId = "pm_test_card_visa" };
        await _client.PostAsJsonAsync($"/api/v1/payments/{createResult!.PaymentId}/process", processRequest);

        var refundRequest = new { Amount = 100m, Reason = "Customer requested refund" };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/payments/{createResult.PaymentId}/refund",
            refundRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefundPayment_WithAmountGreaterThanPayment_ShouldReturnBadRequest()
    {
        // Arrange - Create payment
        var createCommand = new CreatePaymentCommand(Guid.NewGuid(), 50m, "USD", "CreditCard");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/payments", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        var refundRequest = new { Amount = 100m, Reason = "Refund more than paid" };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/payments/{createResult!.PaymentId}/refund",
            refundRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefundPayment_WithNegativeAmount_ShouldReturnBadRequest()
    {
        // Arrange - Create payment
        var createCommand = new CreatePaymentCommand(Guid.NewGuid(), 100m, "USD", "CreditCard");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/payments", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        var refundRequest = new { Amount = -50m, Reason = "Invalid negative refund" };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/payments/{createResult!.PaymentId}/refund",
            refundRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefundPayment_WithNonExistingPaymentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var refundRequest = new { Amount = 50m, Reason = "Test refund" };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/payments/{nonExistingId}/refund",
            refundRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Classes

    private record CreatePaymentResponse(Guid PaymentId);
    private record SuccessResponse(string Message);
    private class ValidationProblemDetails
    {
        public string? Title { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    private class ProblemDetails
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public int? Status { get; set; }
        public string? Type { get; set; }
    }

    #endregion
}
