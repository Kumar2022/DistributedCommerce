using Identity.Application.Commands;
using Identity.Application.DTOs;
using Identity.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Handles user management operations
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IMediator mediator,
        ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", id);

        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("User not found with ID: {UserId}", id);
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound,
                Type = result.Error.Code
            });
        }

        return Ok(result.Value);
    }

    // TODO: Implement update user when UpdateUserCommand is available
    // TODO: Implement verify email when VerifyEmailCommand is available
    // TODO: Implement forgot password when ForgotPasswordCommand is available
    // TODO: Implement reset password when ResetPasswordCommand is available
    // TODO: Implement change password when ChangePasswordCommand is available
    // TODO: Implement delete user when DeleteUserCommand is available
}
