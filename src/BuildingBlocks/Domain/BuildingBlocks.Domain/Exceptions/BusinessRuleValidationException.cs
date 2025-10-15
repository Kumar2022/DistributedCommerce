namespace BuildingBlocks.Domain.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public sealed class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string message) 
        : base(message)
    {
    }

    public BusinessRuleValidationException(string ruleName, string message) 
        : base($"Business rule '{ruleName}' violated: {message}")
    {
        RuleName = ruleName;
    }

    public string? RuleName { get; }
}
