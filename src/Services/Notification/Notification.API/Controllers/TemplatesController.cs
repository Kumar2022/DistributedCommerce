using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Commands;
using Notification.Application.Queries;
using Notification.Application.DTOs;
using Notification.Domain.ValueObjects;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/v1/templates")]
public class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        IMediator mediator,
        ILogger<TemplatesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NotificationChannel>(request.Channel, true, out var channel))
        {
            return BadRequest($"Invalid channel: {request.Channel}");
        }

        var command = new CreateTemplateCommand(
            request.Name,
            request.Description,
            channel,
            request.SubjectTemplate,
            request.BodyTemplate,
            "system", // TODO: Get from authenticated user
            request.Category,
            request.DefaultVariables
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(
            nameof(GetTemplate),
            new { id = result.Value },
            result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NotificationTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetTemplateByIdQuery(id);
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

    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTemplates(
        CancellationToken cancellationToken)
    {
        var query = new GetActiveTemplatesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(
        Guid id,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTemplateCommand(
            id,
            request.Name,
            request.Description,
            request.SubjectTemplate,
            request.BodyTemplate,
            "system", // TODO: Get from authenticated user
            request.Category,
            request.DefaultVariables
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    [HttpPatch("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ActivateTemplateCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    [HttpPatch("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateTemplateCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }
}

// Request DTOs
public record CreateTemplateRequest(
    string Name,
    string Description,
    string Channel,
    string SubjectTemplate,
    string BodyTemplate,
    string? Category = null,
    Dictionary<string, string>? DefaultVariables = null
);

public record UpdateTemplateRequest(
    string Name,
    string Description,
    string SubjectTemplate,
    string BodyTemplate,
    string? Category = null,
    Dictionary<string, string>? DefaultVariables = null
);
