using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior that logs request execution with structured logging
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "Handling {RequestName} with data {@Request}",
            requestName,
            request);

        try
        {
            var response = await next();

            if (response.IsSuccess)
            {
                logger.LogInformation(
                    "Successfully handled {RequestName}",
                    requestName);
            }
            else
            {
                logger.LogWarning(
                    "Handled {RequestName} with error: {ErrorCode} - {ErrorMessage}",
                    requestName,
                    response.Error.Code,
                    response.Error.Message);
            }

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error handling {RequestName}",
                requestName);

            throw;
        }
    }
}
