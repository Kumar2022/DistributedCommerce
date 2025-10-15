using Notification.Application.Commands;
using Notification.Application.IntegrationEvents;
using MediatR;

namespace Notification.Application.EventHandlers;

/// <summary>
/// Handles OrderCreated events and sends notification to customer
/// </summary>
public class OrderCreatedEventHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(
        IMediator mediator,
        ILogger<OrderCreatedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing OrderCreated event for Order {OrderId}, Customer {CustomerId}. " +
                "In production, this would send an order confirmation email with {ItemCount} items, total amount: {TotalAmount} {Currency}",
                @event.OrderId, @event.CustomerId, @event.Items.Count, @event.TotalAmount, @event.Currency);

            // NOTE: In production, you would:
            // 1. Query customer info (email, name) from Identity service
            // 2. Use a template for order confirmation
            // 3. Send the notification via SendNotificationCommand
            // Example (commented out until Identity integration is complete):
            /*
            var customer = await _identityService.GetCustomerAsync(@event.CustomerId, cancellationToken);
            var command = new SendNotificationCommand(
                UserId: @event.CustomerId,
                Email: customer.Email,
                Name: customer.Name,
                PhoneNumber: customer.PhoneNumber,
                Channel: NotificationChannel.Email,
                Subject: $"Order Confirmation - #{@event.OrderId.ToString()[..8]}",
                Body: BuildOrderConfirmationBody(@event),
                Priority: NotificationPriority.Normal,
                Variables: new Dictionary<string, string>
                {
                    ["OrderId"] = @event.OrderId.ToString(),
                    ["TotalAmount"] = @event.TotalAmount.ToString("F2"),
                    ["Currency"] = @event.Currency,
                    ["ItemCount"] = @event.Items.Count.ToString()
                }
            );
            await _mediator.Send(command, cancellationToken);
            */

            _logger.LogInformation(
                "OrderCreated notification handler executed successfully for Order {OrderId}",
                @event.OrderId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error handling OrderCreated event for Order {OrderId}", 
                @event.OrderId);
            // Don't throw - notification failures should not block the saga
        }
    }
}

/// <summary>
/// Handles OrderShipped events and sends notification to customer
/// </summary>
public class OrderShippedEventHandler : IIntegrationEventHandler<OrderShippedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderShippedEventHandler> _logger;

    public OrderShippedEventHandler(
        IMediator mediator,
        ILogger<OrderShippedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(OrderShippedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing OrderShipped event for Order {OrderId}, Customer {CustomerId}. " +
                "In production, this would send a shipping notification with tracking number: {TrackingNumber}",
                @event.OrderId, @event.CustomerId, @event.TrackingNumber);

            // NOTE: Similar to OrderCreated, in production you would send actual notifications
            // See OrderCreatedEventHandler for implementation pattern

            _logger.LogInformation(
                "OrderShipped notification handler executed successfully for Order {OrderId}",
                @event.OrderId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error handling OrderShipped event for Order {OrderId}", 
                @event.OrderId);
        }
    }
}

/// <summary>
/// Handles PaymentCompleted events and sends notification to customer
/// </summary>
public class PaymentCompletedEventHandler : IIntegrationEventHandler<PaymentCompletedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentCompletedEventHandler> _logger;

    public PaymentCompletedEventHandler(
        IMediator mediator,
        ILogger<PaymentCompletedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentCompletedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing PaymentCompleted event for Payment {PaymentId}, Order {OrderId}, Customer {CustomerId}. " +
                "In production, this would send a payment receipt for amount: {Amount} {Currency}",
                @event.PaymentId, @event.OrderId, @event.CustomerId, @event.Amount, @event.Currency);

            // NOTE: See OrderCreatedEventHandler for notification implementation pattern

            _logger.LogInformation(
                "PaymentCompleted notification handler executed successfully for Payment {PaymentId}",
                @event.PaymentId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error handling PaymentCompleted event for Payment {PaymentId}", 
                @event.PaymentId);
        }
    }
}

/// <summary>
/// Handles PaymentFailed events and sends notification to customer
/// </summary>
public class PaymentFailedEventHandler : IIntegrationEventHandler<PaymentFailedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentFailedEventHandler> _logger;

    public PaymentFailedEventHandler(
        IMediator mediator,
        ILogger<PaymentFailedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentFailedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing PaymentFailed event for Payment {PaymentId}, Order {OrderId}, Customer {CustomerId}. " +
                "In production, this would send a payment failure notification. Reason: {Reason}",
                @event.PaymentId, @event.OrderId, @event.CustomerId, @event.Reason);

            // NOTE: See OrderCreatedEventHandler for notification implementation pattern
            // For payment failures, priority should be High

            _logger.LogInformation(
                "PaymentFailed notification handler executed successfully for Payment {PaymentId}",
                @event.PaymentId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error handling PaymentFailed event for Payment {PaymentId}", 
                @event.PaymentId);
        }
    }
}

/// <summary>
/// Handles OrderCancelled events and sends notification to customer
/// </summary>
public class OrderCancelledEventHandler : IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderCancelledEventHandler> _logger;

    public OrderCancelledEventHandler(
        IMediator mediator,
        ILogger<OrderCancelledEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCancelledIntegrationEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing OrderCancelled event for Order {OrderId}, Customer {CustomerId}. " +
                "In production, this would send an order cancellation notification. Reason: {Reason}",
                @event.OrderId, @event.CustomerId, @event.Reason);

            // NOTE: See OrderCreatedEventHandler for notification implementation pattern

            _logger.LogInformation(
                "OrderCancelled notification handler executed successfully for Order {OrderId}",
                @event.OrderId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error handling OrderCancelled event for Order {OrderId}", 
                @event.OrderId);
        }
    }
}
