namespace Identity.Application.Commands;

/// <summary>
/// Command to register a new user
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : ICommand<Guid>;
