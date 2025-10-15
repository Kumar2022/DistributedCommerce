namespace BuildingBlocks.Infrastructure.Time;

/// <summary>
/// Provides current date and time (useful for testing)
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

/// <summary>
/// Production implementation that returns the actual current time
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
