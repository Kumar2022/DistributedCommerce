using MediatR;

namespace BuildingBlocks.Application.Commands;

/// <summary>
/// Marker interface for commands that don't return a value
/// Commands represent intentions to change state in the system
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Interface for commands that return a value
/// </summary>
/// <typeparam name="TResponse">The type of the response</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
