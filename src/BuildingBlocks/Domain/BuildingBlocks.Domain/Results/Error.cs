namespace BuildingBlocks.Domain.Results;

/// <summary>
/// Represents an error with a code and message
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    
    public static readonly Error NullValue = new(
        "Error.NullValue",
        "The specified value is null");

    public static Error NotFound(string message) => new(
        "NotFound",
        message);

    public static Error NotFound(string entityName, object id) => new(
        $"{entityName}.NotFound",
        $"{entityName} with ID '{id}' was not found");

    public static Error Validation(string propertyName, string message) => new(
        $"Validation.{propertyName}",
        message);

    public static Error Conflict(string message) => new(
        "Conflict",
        message);

    public static Error Unauthorized(string message = "Unauthorized access") => new(
        "Unauthorized",
        message);

    public static Error Forbidden(string message = "Forbidden") => new(
        "Forbidden",
        message);

    public static Error Unexpected(string message = "An unexpected error occurred") => new(
        "Unexpected",
        message);

    public static implicit operator string(Error error) => error.Code;
}
