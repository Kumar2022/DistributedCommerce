using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior that measures and logs request execution performance
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly Stopwatch _stopwatch = new();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _stopwatch.Start();

        var response = await next();

        _stopwatch.Stop();

        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;

        if (elapsedMilliseconds <= 500) return response; // Log if request takes more than 500ms
        var requestName = typeof(TRequest).Name;

        logger.LogWarning(
            "Long running request: {RequestName} took {ElapsedMilliseconds}ms with data {@Request}",
            requestName,
            elapsedMilliseconds,
            request);

        return response;
    }
}
