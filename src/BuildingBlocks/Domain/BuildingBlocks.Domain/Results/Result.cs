namespace BuildingBlocks.Domain.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail
/// Implements Railway-Oriented Programming pattern
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        switch (isSuccess)
        {
            case true when error != Error.None:
                throw new InvalidOperationException("Successful result cannot have an error");
            case false when error == Error.None:
                throw new InvalidOperationException("Failed result must have an error");
            default:
                IsSuccess = isSuccess;
                Error = error;
                break;
        }
    }

    public bool IsSuccess { get; }
    
    public bool IsFailure => !IsSuccess;
    
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    
    public static Result Failure(Error error) => new(false, error);
    
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static Result Create(bool condition, Error error)
        => condition ? Success() : Failure(error);

    public static Result<TValue> Create<TValue>(TValue? value, Error error) where TValue : class
        => value is not null ? Success(value) : Failure<TValue>(error);
}

/// <summary>
/// Represents the result of an operation that returns a value
/// </summary>
/// <typeparam name="TValue">The type of the value</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error) 
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Cannot access value of a failed result");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}
