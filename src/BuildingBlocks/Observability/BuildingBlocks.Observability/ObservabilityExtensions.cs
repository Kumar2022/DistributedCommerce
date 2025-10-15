using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Observability;

/// <summary>
/// Extension methods for adding distributed tracing and metrics using OpenTelemetry
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing, metrics, and logging for distributed observability
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceName">The name of the service (e.g., "order-service")</param>
    /// <param name="serviceVersion">The version of the service (e.g., "1.0.0")</param>
    /// <param name="otlpEndpoint">The OTLP endpoint URL (e.g., "http://localhost:4317")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDistributedObservability(
        this IServiceCollection services,
        string serviceName,
        string serviceVersion = "1.0.0",
        string? otlpEndpoint = null)
    {
        // Default to localhost if not provided
        otlpEndpoint ??= "http://localhost:4317";

        // Configure resource attributes
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();

        // Add OpenTelemetry Tracing
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Enrich spans with additional information
                        options.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            activity.SetTag("http.request_content_length", httpRequest.ContentLength);
                            activity.SetTag("http.request_content_type", httpRequest.ContentType);
                        };
                        
                        options.EnrichWithHttpResponse = (activity, httpResponse) =>
                        {
                            activity.SetTag("http.response_content_length", httpResponse.ContentLength);
                            activity.SetTag("http.response_content_type", httpResponse.ContentType);
                        };
                        
                        // Filter out health check endpoints
                        options.Filter = context =>
                        {
                            return !context.Request.Path.Value?.Contains("/health") ?? true;
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        // Enrich HTTP client spans
                        options.EnrichWithHttpRequestMessage = (activity, httpRequest) =>
                        {
                            activity.SetTag("http.client.method", httpRequest.Method.ToString());
                        };
                        
                        options.EnrichWithHttpResponseMessage = (activity, httpResponse) =>
                        {
                            activity.SetTag("http.client.status_code", (int)httpResponse.StatusCode);
                        };
                    })
                    .AddSqlClientInstrumentation(options =>
                    {
                        // Record SQL queries
                        options.RecordException = true;
                    })
                    // Add custom instrumentation sources
                    .AddSource("BuildingBlocks.*")
                    .AddSource("Order.*")
                    .AddSource("Inventory.*")
                    .AddSource("Payment.*")
                    .AddSource("Catalog.*")
                    .AddSource("Shipping.*")
                    .AddSource("Analytics.*")
                    .AddSource("Notification.*")
                    // Export to OTLP (OpenTelemetry Protocol)
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    // Add custom meters
                    .AddMeter("BuildingBlocks.*")
                    .AddMeter("Order.*")
                    .AddMeter("Inventory.*")
                    .AddMeter("Payment.*")
                    .AddMeter("Catalog.*")
                    .AddMeter("Shipping.*")
                    .AddMeter("Analytics.*")
                    .AddMeter("Notification.*")
                    // Export to OTLP
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
            });

        // Configure logging to use OpenTelemetry
        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
                
                // Include formatted message, scopes, and state values
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                
                // Export to OTLP
                options.AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = new Uri(otlpEndpoint);
                    exporter.Protocol = OtlpExportProtocol.Grpc;
                });
            });
        });

        return services;
    }

    /// <summary>
    /// Adds custom meters for business metrics
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceName">The name of the service</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBusinessMetrics(
        this IServiceCollection services,
        string serviceName)
    {
        services.AddSingleton(new BusinessMetrics(serviceName));
        return services;
    }
}

/// <summary>
/// Custom business metrics for tracking domain-specific operations
/// </summary>
public class BusinessMetrics
{
    private readonly System.Diagnostics.Metrics.Meter _meter;
    private readonly System.Diagnostics.Metrics.Counter<long> _orderCreatedCounter;
    private readonly System.Diagnostics.Metrics.Counter<long> _orderCancelledCounter;
    private readonly System.Diagnostics.Metrics.Counter<long> _paymentSuccessCounter;
    private readonly System.Diagnostics.Metrics.Counter<long> _paymentFailedCounter;
    private readonly System.Diagnostics.Metrics.Histogram<double> _orderValueHistogram;
    private readonly System.Diagnostics.Metrics.Histogram<double> _sagaDurationHistogram;

    public BusinessMetrics(string serviceName)
    {
        _meter = new System.Diagnostics.Metrics.Meter($"{serviceName}.Business", "1.0.0");
        
        // Counters
        _orderCreatedCounter = _meter.CreateCounter<long>(
            "orders.created",
            unit: "orders",
            description: "Number of orders created");
        
        _orderCancelledCounter = _meter.CreateCounter<long>(
            "orders.cancelled",
            unit: "orders",
            description: "Number of orders cancelled");
        
        _paymentSuccessCounter = _meter.CreateCounter<long>(
            "payments.succeeded",
            unit: "payments",
            description: "Number of successful payments");
        
        _paymentFailedCounter = _meter.CreateCounter<long>(
            "payments.failed",
            unit: "payments",
            description: "Number of failed payments");
        
        // Histograms
        _orderValueHistogram = _meter.CreateHistogram<double>(
            "order.value",
            unit: "currency",
            description: "Distribution of order values");
        
        _sagaDurationHistogram = _meter.CreateHistogram<double>(
            "saga.duration",
            unit: "ms",
            description: "Duration of saga executions");
    }

    public void RecordOrderCreated(string status = "pending") =>
        _orderCreatedCounter.Add(1, new KeyValuePair<string, object?>("status", status));

    public void RecordOrderCancelled(string reason) =>
        _orderCancelledCounter.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecordPaymentSuccess(string paymentMethod) =>
        _paymentSuccessCounter.Add(1, new KeyValuePair<string, object?>("method", paymentMethod));

    public void RecordPaymentFailed(string reason) =>
        _paymentFailedCounter.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecordOrderValue(double value, string currency) =>
        _orderValueHistogram.Record(value, new KeyValuePair<string, object?>("currency", currency));

    public void RecordSagaDuration(double durationMs, string sagaName, string status) =>
        _sagaDurationHistogram.Record(durationMs, 
            new KeyValuePair<string, object?>("saga", sagaName),
            new KeyValuePair<string, object?>("status", status));
}
