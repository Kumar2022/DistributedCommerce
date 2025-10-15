using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring observability (OpenTelemetry)
/// </summary>
public static class ObservabilityExtensions
{
    public static IServiceCollection AddGatewayObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource
                    .AddService("ApiGateway", serviceVersion: "1.0.0")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = environment.EnvironmentName,
                        ["service.namespace"] = "DistributedCommerce",
                        ["service.instance.id"] = Environment.MachineName
                    });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            // Don't trace health checks and metrics endpoints
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !path.Contains("/health") && !path.Contains("/metrics");
                        };
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                            activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response_content_length", response.ContentLength);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddSource("ApiGateway")
                    .AddConsoleExporter();

                // Add OTLP exporter if configured
                var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("ApiGateway")
                    .AddConsoleExporter();

                // Add OTLP exporter if configured
                var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });

        return services;
    }
}
