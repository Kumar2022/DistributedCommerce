namespace Identity.Application.Commands;

/// <summary>
/// Command to login a user
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<string>; // Returns JWT token
