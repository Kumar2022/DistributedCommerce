using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Commands;
using Payment.Application.DTOs;
using Payment.Application.Queries;
using BuildingBlocks.Authentication.Services;

namespace Payment.API.Controllers;

/// <summary>
/// Handles payment processing operations with Stripe integration
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;
    private readonly ICurrentUserService _currentUser;

    public PaymentsController(
        IMediator mediator,
        ILogger<PaymentsController> logger,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Create a new payment
    /// </summary>
    /// <param name="command">Payment creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payment ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} creating payment for order: {OrderId}, Amount: {Amount} {Currency}",
            _currentUser.UserId, command.OrderId, command.Amount, command.Currency);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Payment creation failed for order {OrderId}: {Error}",
                command.OrderId, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Payment Creation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Payment created successfully with ID: {PaymentId}", result.Value);
        return CreatedAtAction(
            nameof(GetPaymentById),
            new { id = result.Value },
            new { paymentId = result.Value });
    }

    /// <summary>
    /// Get payment by ID
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving payment: {PaymentId}", id);

        var query = new GetPaymentByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Payment not found: {PaymentId}", id);
            return NotFound(new ProblemDetails
            {
                Title = "Payment Not Found",
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound,
                Type = result.Error.Code
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get payments by order ID
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payments for the order</returns>
    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentsByOrderId(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving payments for order: {OrderId}", orderId);

        var query = new GetPaymentsByOrderIdQuery(orderId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("No payments found for order: {OrderId}", orderId);
            return NotFound(new ProblemDetails
            {
                Title = "Payments Not Found",
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound,
                Type = result.Error.Code
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Process a payment with Stripe
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="request">Processing request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessPayment(
        Guid id,
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment: {PaymentId} with method: {PaymentMethodId}",
            id, request.PaymentMethodId);

        var command = new ProcessPaymentCommand(id, request.PaymentMethodId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Payment processing failed for {PaymentId}: {Error}",
                id, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Payment Processing Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Payment processed successfully: {PaymentId}", id);
        return Ok(new { message = "Payment processed successfully" });
    }

    /// <summary>
    /// Confirm a payment after successful processing
    /// </summary>
    /// <param name="request">Confirmation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Confirming payment with external ID: {ExternalPaymentId}",
            request.ExternalPaymentId);

        var command = new ConfirmPaymentCommand(request.ExternalPaymentId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Payment confirmation failed for external ID {ExternalPaymentId}: {Error}",
                request.ExternalPaymentId, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Payment Confirmation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Payment confirmed successfully with external ID: {ExternalPaymentId}", 
            request.ExternalPaymentId);
        return Ok(new { message = "Payment confirmed successfully" });
    }

    /// <summary>
    /// Refund a payment
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="request">Refund details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefundPayment(
        Guid id,
        [FromBody] RefundPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refunding payment: {PaymentId}, Amount: {Amount}, Reason: {Reason}",
            id, request.Amount, request.Reason);

        var command = new RefundPaymentCommand(id, request.Amount, request.Reason);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Payment refund failed for {PaymentId}: {Error}",
                id, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Payment Refund Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Payment refunded successfully: {PaymentId}", id);
        return Ok(new { message = "Payment refunded successfully" });
    }

    /// <summary>
    /// Handle Stripe webhook events
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("/webhooks/stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StripeWebhook(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received Stripe webhook");

        // Read the raw body for signature verification
        using var reader = new StreamReader(HttpContext.Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);

        // TODO: Implement Stripe webhook signature verification and processing
        // This would typically:
        // 1. Verify the webhook signature
        // 2. Parse the event
        // 3. Process based on event type (payment_intent.succeeded, payment_intent.payment_failed, etc.)
        // 4. Update payment status accordingly

        _logger.LogInformation("Stripe webhook processed successfully");
        return Ok();
    }
}

/// <summary>
/// Request DTO for processing payment
/// </summary>
public record ProcessPaymentRequest(string PaymentMethodId);

/// <summary>
/// Request DTO for confirming payment
/// </summary>
public record ConfirmPaymentRequest(string ExternalPaymentId);

/// <summary>
/// Request DTO for refunding payment
/// </summary>
public record RefundPaymentRequest(decimal Amount, string Reason);
