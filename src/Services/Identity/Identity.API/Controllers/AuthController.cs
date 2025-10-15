using Identity.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Handles authentication operations including user registration and login
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController(
    IMediator mediator,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="command">User registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user ID</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Registering new user with email: {Email}", command.Email);

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            logger.LogWarning("User registration failed for email {Email}: {Error}", 
                command.Email, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        logger.LogInformation("User registered successfully with ID: {UserId}", result.Value);
        return CreatedAtAction(
            nameof(UsersController.GetById),
            "Users",
            new { id = result.Value },
            new { userId = result.Value });
    }

    /// <summary>
    /// Log in a user
    /// </summary>
    /// <param name="command">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Login attempt for user: {Email}", command.Email);

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Login failed for user {Email}: {Error}", 
                command.Email, result.Error.Message);
            return Unauthorized(new ProblemDetails
            {
                Title = "Login Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status401Unauthorized,
                Type = result.Error.Code
            });
        }

        logger.LogInformation("User logged in successfully: {Email}", command.Email);
        return Ok(new
        {
            token = result.Value,
            tokenType = "Bearer"
        });
    }

    // TODO: Implement refresh token when RefreshTokenCommand is available
    // TODO: Implement logout when LogoutCommand is available
}
