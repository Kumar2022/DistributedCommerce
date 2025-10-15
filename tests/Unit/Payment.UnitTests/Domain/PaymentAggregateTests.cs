using AutoFixture;
using Bogus;
using BuildingBlocks.Domain.Results;
using FluentAssertions;
using Payment.Domain.Aggregates.PaymentAggregate;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Xunit;
using PaymentEntity = Payment.Domain.Aggregates.PaymentAggregate.Payment;

namespace Payment.UnitTests.Domain;

/// <summary>
/// Comprehensive unit tests for Payment aggregate root
/// Tests payment processing lifecycle, refunds, and state transitions
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class PaymentAggregateTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;

    public PaymentAggregateTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
    }

    #region Payment Creation Tests

    [Fact(DisplayName = "Create Payment: With valid data should succeed")]
    public void CreatePayment_WithValidData_ShouldSucceed()
    {
        // Arrange
        var orderId = OrderId.Create(Guid.NewGuid());
        var amountResult = Money.Create(100.00m, "USD");
        var method = PaymentMethod.CreditCard;

        // Act
        var result = PaymentEntity.Create(
            orderId,
            amountResult.Value,
            method);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        payment.Should().NotBeNull();
        payment.Id.Should().NotBe(Guid.Empty);
        payment.OrderId.Should().Be(orderId);
        payment.Amount.Should().Be(amountResult.Value);
        payment.Method.Should().Be(method);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.RefundedAmount.Should().Be(0);
    }

    [Theory(DisplayName = "Create Payment: With different payment methods")]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.PayPal)]
    [InlineData(PaymentMethod.BankTransfer)]
    public void CreatePayment_WithDifferentPaymentMethods_ShouldSucceed(PaymentMethod method)
    {
        // Arrange
        var orderId = OrderId.Create(Guid.NewGuid());
        var amountResult = Money.Create(100.00m, "USD");

        // Act
        var result = PaymentEntity.Create(
            orderId,
            amountResult.Value,
            method);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Method.Should().Be(method);
    }

    [Theory(DisplayName = "Create Payment: With various amounts")]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(100.00)]
    [InlineData(9999.99)]
    public void CreatePayment_WithVariousAmounts_ShouldSucceed(decimal amount)
    {
        // Arrange
        var orderId = OrderId.Create(Guid.NewGuid());
        var moneyResult = Money.Create(amount, "USD");
        var method = PaymentMethod.CreditCard;

        // Act
        var result = PaymentEntity.Create(
            orderId,
            moneyResult.Value,
            method);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Amount.Should().Be(amount);
    }

    #endregion

    #region Payment Processing Tests

    [Fact(DisplayName = "Initiate Processing: On pending payment should succeed")]
    public void InitiateProcessing_OnPendingPayment_ShouldSucceed()
    {
        // Arrange
        var payment = CreateValidPayment();
        var externalPaymentId = "stripe_" + Guid.NewGuid();

        // Act
        var result = payment.InitiateProcessing(externalPaymentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.ExternalPaymentId.Should().Be(externalPaymentId);
    }

    [Fact(DisplayName = "Initiate Processing: On non-pending payment should fail")]
    public void InitiateProcessing_OnNonPendingPayment_ShouldFail()
    {
        // Arrange
        var payment = CreateValidPayment();
        payment.InitiateProcessing("ext_123");
        payment.MarkAsSucceeded();

        // Act
        var result = payment.InitiateProcessing("ext_456");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Conflict");
    }

    [Theory(DisplayName = "Initiate Processing: With invalid external ID should fail")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void InitiateProcessing_WithInvalidExternalId_ShouldFail(string invalidId)
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        var result = payment.InitiateProcessing(invalidId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    #endregion

    #region Payment Success Tests

    [Fact(DisplayName = "Mark As Succeeded: From processing should succeed")]
    public void MarkAsSucceeded_FromProcessing_ShouldSucceed()
    {
        // Arrange
        var payment = CreateProcessingPayment();

        // Act
        var result = payment.MarkAsSucceeded();

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.ProcessedAt.Should().NotBeNull();
        payment.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact(DisplayName = "Mark As Succeeded: From pending should fail")]
    public void MarkAsSucceeded_FromPending_ShouldFail()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        var result = payment.MarkAsSucceeded();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Conflict");
    }

    [Fact(DisplayName = "Mark As Succeeded: From failed should fail")]
    public void MarkAsSucceeded_FromFailed_ShouldFail()
    {
        // Arrange
        var payment = CreateProcessingPayment();
        payment.MarkAsFailed("Test failure", "ERR_001");

        // Act
        var result = payment.MarkAsSucceeded();

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Payment Failure Tests

    [Fact(DisplayName = "Mark As Failed: From processing should succeed")]
    public void MarkAsFailed_FromProcessing_ShouldSucceed()
    {
        // Arrange
        var payment = CreateProcessingPayment();
        var reason = "Insufficient funds";
        var errorCode = "ERR_INSUFFICIENT_FUNDS";

        // Act
        var result = payment.MarkAsFailed(reason, errorCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be(reason);
        payment.ErrorCode.Should().Be(errorCode);
        payment.FailedAt.Should().NotBeNull();
        payment.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact(DisplayName = "Mark As Failed: From pending should succeed")]
    public void MarkAsFailed_FromPending_ShouldSucceed()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        var result = payment.MarkAsFailed("Invalid card", "ERR_INVALID_CARD");

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact(DisplayName = "Mark As Failed: From succeeded should fail")]
    public void MarkAsFailed_FromSucceeded_ShouldFail()
    {
        // Arrange
        var payment = CreateSucceededPayment();

        // Act
        var result = payment.MarkAsFailed("Cannot fail", "ERR_001");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Conflict");
        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    #endregion

    #region Payment Refund Tests

    [Fact(DisplayName = "Refund Payment: Full refund should succeed")]
    public void RefundPayment_FullRefund_ShouldSucceed()
    {
        // Arrange
        var payment = CreateSucceededPayment();
        var originalAmount = payment.Amount.Amount;
        var reason = "Customer requested refund";

        // Act
        var result = payment.Refund(originalAmount, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmount.Should().Be(originalAmount);
        payment.RefundedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "Refund Payment: Partial refund should succeed")]
    public void RefundPayment_PartialRefund_ShouldSucceed()
    {
        // Arrange
        var payment = CreateSucceededPayment();
        var refundAmount = 25.00m;
        var reason = "Partial refund for damaged item";

        // Act
        var result = payment.Refund(refundAmount, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.RefundedAmount.Should().Be(refundAmount);
        payment.RefundedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "Refund Payment: Multiple partial refunds should succeed")]
    public void RefundPayment_MultiplePartialRefunds_ShouldSucceed()
    {
        // Arrange
        var payment = CreateSucceededPayment();
        var firstRefund = 20.00m;
        var secondRefund = 30.00m;

        // Act
        var result1 = payment.Refund(firstRefund, "First refund");
        var result2 = payment.Refund(secondRefund, "Second refund");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        payment.RefundedAmount.Should().Be(firstRefund + secondRefund);
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
    }

    [Fact(DisplayName = "Refund Payment: Multiple refunds totaling full amount")]
    public void RefundPayment_MultipleRefundsTotalingFullAmount_ShouldSucceed()
    {
        // Arrange
        var payment = CreateSucceededPayment();
        var amount = payment.Amount.Amount;
        var firstRefund = amount * 0.6m;  // 60%
        var secondRefund = amount * 0.4m; // 40%

        // Act
        var result1 = payment.Refund(firstRefund, "First refund");
        var result2 = payment.Refund(secondRefund, "Second refund");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        payment.RefundedAmount.Should().Be(amount);
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact(DisplayName = "Refund Payment: Exceeding payment amount should fail")]
    public void RefundPayment_ExceedingPaymentAmount_ShouldFail()
    {
        // Arrange
        var payment = CreateSucceededPayment();
        var originalAmount = payment.Amount.Amount;
        var excessiveRefund = originalAmount + 10.00m;

        // Act
        var result = payment.Refund(excessiveRefund, "Too much");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
        payment.RefundedAmount.Should().Be(0);
        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Theory(DisplayName = "Refund Payment: With invalid amount should fail")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void RefundPayment_WithInvalidAmount_ShouldFail(decimal invalidAmount)
    {
        // Arrange
        var payment = CreateSucceededPayment();

        // Act
        var result = payment.Refund(invalidAmount, "Invalid refund");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    [Fact(DisplayName = "Refund Payment: On non-succeeded payment should fail")]
    public void RefundPayment_OnNonSucceededPayment_ShouldFail()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        var result = payment.Refund(50.00m, "Cannot refund");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Conflict");
    }

    [Fact(DisplayName = "Refund Payment: On failed payment should fail")]
    public void RefundPayment_OnFailedPayment_ShouldFail()
    {
        // Arrange
        var payment = CreateProcessingPayment();
        payment.MarkAsFailed("Card declined", "ERR_DECLINED");

        // Act
        var result = payment.Refund(50.00m, "Cannot refund");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region State Transition Tests

    [Fact(DisplayName = "State Transitions: Complete payment flow should succeed")]
    public void StateTransitions_CompletePaymentFlow_ShouldSucceed()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act & Assert - Step through complete flow
        payment.Status.Should().Be(PaymentStatus.Pending);

        var initiateResult = payment.InitiateProcessing("ext_123");
        initiateResult.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Processing);

        var succeededResult = payment.MarkAsSucceeded();
        succeededResult.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact(DisplayName = "State Transitions: Payment with refund flow")]
    public void StateTransitions_PaymentWithRefundFlow_ShouldSucceed()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act & Assert
        payment.InitiateProcessing("ext_123");
        payment.MarkAsSucceeded();
        payment.Status.Should().Be(PaymentStatus.Succeeded);

        var refundResult = payment.Refund(25.00m, "Partial refund");
        refundResult.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
    }

    [Fact(DisplayName = "State Transitions: Payment failure flow")]
    public void StateTransitions_PaymentFailureFlow_ShouldSucceed()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act & Assert
        payment.InitiateProcessing("ext_123");
        payment.Status.Should().Be(PaymentStatus.Processing);

        var failResult = payment.MarkAsFailed("Card expired", "ERR_EXPIRED");
        failResult.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    #endregion

    #region Helper Methods

    private PaymentEntity CreateValidPayment()
    {
        var orderId = OrderId.Create(Guid.NewGuid());
        var amountResult = Money.Create(100.00m, "USD");
        var method = PaymentMethod.CreditCard;

        var result = PaymentEntity.Create(
            orderId,
            amountResult.Value,
            method);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private PaymentEntity CreateProcessingPayment()
    {
        var payment = CreateValidPayment();
        payment.InitiateProcessing("ext_" + Guid.NewGuid().ToString());
        return payment;
    }

    private PaymentEntity CreateSucceededPayment()
    {
        var payment = CreateProcessingPayment();
        payment.MarkAsSucceeded();
        return payment;
    }

    #endregion
}
