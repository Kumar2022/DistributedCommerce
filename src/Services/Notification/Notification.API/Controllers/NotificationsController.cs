using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Commands;
using Notification.Application.Queries;
using Notification.Application.DTOs;
using Notification.Domain.ValueObjects;
using BuildingBlocks.Authentication.Services;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotificationsController> _logger;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(
        IMediator mediator,
        ILogger<NotificationsController> logger,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _logger = logger;
        _currentUser = currentUser;
    }

    [HttpPost("send")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNotification(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NotificationChannel>(request.Channel, true, out var channel))
        {
            return BadRequest($"Invalid channel: {request.Channel}");
        }

        if (!Enum.TryParse<NotificationPriority>(request.Priority, true, out var priority))
        {
            priority = NotificationPriority.Normal;
        }

        var command = new SendNotificationCommand(
            request.UserId,
            request.Email,
            request.Name,
            request.PhoneNumber,
            channel,
            request.Subject,
            request.Body,
            priority,
            request.TemplateId,
            request.Variables
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(
            nameof(GetNotification),
            new { id = result.Value },
            result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotification(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }

        if (result.Value == null)
        {
            return NotFound();
        }

        return Ok(result.Value);
    }

    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserNotifications(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationsByUserIdQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost("{id}/mark-sent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsSent(
        Guid id,
        [FromBody] MarkSentRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new MarkNotificationAsSentCommand(id, request?.ExternalId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    [HttpPost("{id}/mark-delivered")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsDelivered(
        Guid id,
        [FromBody] MarkDeliveredRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new MarkNotificationAsDeliveredCommand(id, request?.ExternalId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }
}

// Request DTOs
public record SendNotificationRequest(
    Guid UserId,
    string Email,
    string Name,
    string? PhoneNumber,
    string Channel,
    string Subject,
    string Body,
    string? Priority = null,
    Guid? TemplateId = null,
    Dictionary<string, string>? Variables = null
);

public record MarkSentRequest(string? ExternalId);
public record MarkDeliveredRequest(string? ExternalId);
