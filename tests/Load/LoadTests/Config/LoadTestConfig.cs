namespace LoadTests.Config;

/// <summary>
/// Configuration for load tests
/// </summary>
public class LoadTestConfig
{
    public string ApiGatewayUrl { get; set; } = "http://localhost:5000";
    public string CatalogServiceUrl { get; set; } = "http://localhost:5050";
    public string OrderServiceUrl { get; set; } = "http://localhost:5051";
    public string PaymentServiceUrl { get; set; } = "http://localhost:5052";
    public string InventoryServiceUrl { get; set; } = "http://localhost:5053";
    public string IdentityServiceUrl { get; set; } = "http://localhost:5054";
    public string ShippingServiceUrl { get; set; } = "http://localhost:5055";
    public string NotificationServiceUrl { get; set; } = "http://localhost:5056";
    public string AnalyticsServiceUrl { get; set; } = "http://localhost:5057";
    
    public int WarmUpDuration { get; set; } = 5; // seconds
    public int TestDuration { get; set; } = 30; // seconds
    public int CoolDownDuration { get; set; } = 5; // seconds
    
    public int ConcurrentUsers { get; set; } = 50;
    public int MaxConcurrentUsers { get; set; } = 500;
    
    public int RequestsPerSecond { get; set; } = 100;
    public int MaxRequestsPerSecond { get; set; } = 1000;
    
    public static LoadTestConfig Default => new();
    
    public static LoadTestConfig FromEnvironment()
    {
        return new LoadTestConfig
        {
            ApiGatewayUrl = Environment.GetEnvironmentVariable("API_GATEWAY_URL") ?? "http://localhost:5000",
            CatalogServiceUrl = Environment.GetEnvironmentVariable("CATALOG_SERVICE_URL") ?? "http://localhost:5050",
            OrderServiceUrl = Environment.GetEnvironmentVariable("ORDER_SERVICE_URL") ?? "http://localhost:5051",
            PaymentServiceUrl = Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL") ?? "http://localhost:5052",
            WarmUpDuration = int.Parse(Environment.GetEnvironmentVariable("WARMUP_DURATION") ?? "5"),
            TestDuration = int.Parse(Environment.GetEnvironmentVariable("TEST_DURATION") ?? "30"),
            ConcurrentUsers = int.Parse(Environment.GetEnvironmentVariable("CONCURRENT_USERS") ?? "50")
        };
    }
}
