using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shipping.Application.Commands;
using Shipping.Application.DTOs;
using Shipping.Application.Queries;
using BuildingBlocks.Authentication.Services;

namespace Shipping.API.Controllers;

/// <summary>
/// Shipments API controller
/// Handles shipment management, tracking, and status updates
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ShipmentsController> _logger;
    private readonly ICurrentUserService _currentUser;

    public ShipmentsController(ISender mediator, ILogger<ShipmentsController> logger, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Create a new shipment
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Warehouse")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} creating shipment for order {OrderId}", _currentUser.UserId, command.OrderId);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create shipment: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Shipment created successfully: {ShipmentId}", result.Value.Id);
        return CreatedAtAction(nameof(GetShipmentById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Get shipment by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipmentById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetShipmentByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Shipment not found: {ShipmentId}", id);
            return NotFound(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get shipment by tracking number
    /// </summary>
    [HttpGet("tracking/{trackingNumber}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipmentByTrackingNumber(string trackingNumber, CancellationToken cancellationToken)
    {
        var query = new GetShipmentByTrackingNumberQuery(trackingNumber);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Shipment not found with tracking number: {TrackingNumber}", trackingNumber);
            return NotFound(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get shipments by order ID
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(List<ShipmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShipmentsByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var query = new GetShipmentsByOrderIdQuery(orderId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get pending shipments (paginated)
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<ShipmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingShipments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingShipmentsQuery(pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get delayed shipments (paginated)
    /// </summary>
    [HttpGet("delayed")]
    [ProducesResponseType(typeof(List<ShipmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDelayedShipments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDelayedShipmentsQuery(pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update shipment status
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShipmentStatus(
        Guid id,
        [FromBody] UpdateShipmentStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.ShipmentId)
        {
            return BadRequest("Shipment ID mismatch");
        }

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to update shipment status: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        return Ok();
    }

    /// <summary>
    /// Mark shipment as delivered
    /// </summary>
    [HttpPost("{id:guid}/delivered")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsDelivered(
        Guid id,
        [FromBody] MarkAsDeliveredCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.ShipmentId)
        {
            return BadRequest("Shipment ID mismatch");
        }

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to mark shipment as delivered: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Shipment {ShipmentId} marked as delivered", id);
        return Ok();
    }

    /// <summary>
    /// Cancel shipment
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelShipment(
        Guid id,
        [FromBody] CancelShipmentCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.ShipmentId)
        {
            return BadRequest("Shipment ID mismatch");
        }

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to cancel shipment: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Shipment {ShipmentId} cancelled", id);
        return Ok();
    }

    /// <summary>
    /// Add tracking update
    /// </summary>
    [HttpPost("{id:guid}/tracking")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTrackingUpdate(
        Guid id,
        [FromBody] AddTrackingUpdateCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.ShipmentId)
        {
            return BadRequest("Shipment ID mismatch");
        }

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to add tracking update: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        return Ok();
    }

    /// <summary>
    /// Record delivery attempt
    /// </summary>
    [HttpPost("{id:guid}/delivery-attempt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordDeliveryAttempt(
        Guid id,
        [FromBody] RecordDeliveryAttemptCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.ShipmentId)
        {
            return BadRequest("Shipment ID mismatch");
        }

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to record delivery attempt: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        return Ok();
    }
}
