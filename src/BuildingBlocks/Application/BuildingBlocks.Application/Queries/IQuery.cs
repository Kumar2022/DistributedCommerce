using MediatR;

namespace BuildingBlocks.Application.Queries;

/// <summary>
/// Interface for queries that return data
/// Queries should not modify state - they are read-only operations
/// </summary>
/// <typeparam name="TResponse">The type of data returned</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
