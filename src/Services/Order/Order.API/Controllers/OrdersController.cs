using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Commands;
using Order.Application.DTOs;
using Order.Application.Queries;
using BuildingBlocks.Authentication.Services;

namespace Order.API.Controllers;

/// <summary>
/// Handles order management operations with event sourcing
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;
    private readonly ICurrentUserService _currentUser;

    public OrdersController(
        IMediator mediator,
        ILogger<OrdersController> logger,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    /// <param name="command">Order creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created order ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (userId == null)
        {
            _logger.LogWarning("Unauthorized order creation attempt");
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "User must be authenticated to create an order",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("User {UserId} creating order for customer: {CustomerId}", 
            userId, command.CustomerId);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Order creation failed for customer {CustomerId}: {Error}",
                command.CustomerId, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Order Creation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Order created successfully with ID: {OrderId}", result.Value);
        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = result.Value },
            new { orderId = result.Value });
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("User {UserId} retrieving order: {OrderId}", userId, id);

        var query = new GetOrderByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Order not found: {OrderId}", id);
            return NotFound(new ProblemDetails
            {
                Title = "Order Not Found",
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound,
                Type = result.Error.Code
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get all orders for the current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's orders</returns>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (userId == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "User must be authenticated",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("User {UserId} retrieving their orders", userId);
        
        // TODO: Implement GetOrdersByUserIdQuery
        // For now, return empty list
        return Ok(new List<OrderDto>());
    }

    /// <summary>
    /// Initiate payment for an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="request">Payment initiation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/initiate-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InitiatePayment(
        Guid id,
        [FromBody] InitiatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("User {UserId} initiating payment for order: {OrderId}", userId, id);

        var command = new InitiatePaymentCommand(id, request.PaymentMethod);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Payment initiation failed for order {OrderId}: {Error}",
                id, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Payment Initiation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Payment initiated successfully for order: {OrderId}", id);
        return Ok(new { message = "Payment initiated successfully" });
    }

    /// <summary>
    /// Confirm an order after successful payment
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("User {UserId} confirming order: {OrderId}", userId, id);

        var command = new ConfirmOrderCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Order confirmation failed for {OrderId}: {Error}",
                id, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Order Confirmation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Order confirmed successfully: {OrderId}", id);
        return Ok(new { message = "Order confirmed successfully" });
    }

    /// <summary>
    /// Mark order as shipped with tracking information
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="request">Shipping information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/ship")]
    [Authorize(Roles = "Admin,Warehouse")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ShipOrder(
        Guid id,
        [FromBody] ShipOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("User {UserId} shipping order: {OrderId} with tracking number: {TrackingNumber}",
            userId, id, request.TrackingNumber);

        var command = new ShipOrderCommand(id, request.TrackingNumber, request.Carrier);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Order shipment failed for {OrderId}: {Error}",
                id, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Order Shipment Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Order shipped successfully: {OrderId}", id);
        return Ok(new { message = "Order shipped successfully" });
    }

    /// <summary>
    /// Cancel an order with a reason
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="request">Cancellation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("User {UserId} cancelling order: {OrderId} with reason: {Reason}",
            userId, id, request.Reason);

        var command = new CancelOrderCommand(id, request.Reason);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Order cancellation failed for {OrderId}: {Error}",
                id, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Order Cancellation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Order cancelled successfully: {OrderId}", id);
        return Ok(new { message = "Order cancelled successfully" });
    }
}

/// <summary>
/// Request DTO for initiating payment
/// </summary>
public record InitiatePaymentRequest(string PaymentMethod);

/// <summary>
/// Request DTO for shipping order
/// </summary>
public record ShipOrderRequest(string TrackingNumber, string Carrier);

/// <summary>
/// Request DTO for cancelling order
/// </summary>
public record CancelOrderRequest(string Reason);
