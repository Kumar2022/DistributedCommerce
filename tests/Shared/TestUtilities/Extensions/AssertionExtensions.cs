using FluentAssertions;

namespace TestUtilities.Extensions;

/// <summary>
/// Custom assertion extensions for tests
/// </summary>
public static class AssertionExtensions
{
    public static void ShouldBeSuccess<T>(this Result<T> result)
    {
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    public static void ShouldBeFailure<T>(this Result<T> result)
    {
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    public static void ShouldBeFailureWithError<T>(this Result<T> result, string expectedError)
    {
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(expectedError);
    }

    public static void ShouldHaveValidationError<T>(this Result<T> result, string fieldName)
    {
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainKey(fieldName);
    }
}

// Simple Result type for testing (replace with actual implementation)
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Value { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, string[]> ValidationErrors { get; set; } = new();

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
