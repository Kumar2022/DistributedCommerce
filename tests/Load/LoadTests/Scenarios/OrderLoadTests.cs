using LoadTests.Config;

namespace LoadTests.Scenarios;

/// <summary>
/// Load tests for Order Service
/// Tests order creation, retrieval, and updates under load
/// </summary>
public class OrderLoadTests
{
    private readonly LoadTestConfig _config = LoadTestConfig.FromEnvironment();
    private readonly Faker _faker = new();

    [Fact(Skip = "Run manually for load testing")]
    public void CreateOrder_UnderLoad_ShouldMeetPerformanceTargets()
    {
        var scenario = Scenario.Create("create_order_load", async context =>
        {
            var orderData = new
            {
                userId = Guid.NewGuid(),
                items = new[]
                {
                    new { productId = Guid.NewGuid(), quantity = _faker.Random.Int(1, 5), price = _faker.Random.Decimal(10, 100) }
                },
                shippingAddress = new
                {
                    street = _faker.Address.StreetAddress(),
                    city = _faker.Address.City(),
                    state = _faker.Address.State(),
                    zipCode = _faker.Address.ZipCode(),
                    country = _faker.Address.Country()
                }
            };

            var request = Http.CreateRequest("POST", $"{_config.OrderServiceUrl}/api/orders")
                .WithHeader("Content-Type", "application/json")
                .WithJsonBody(orderData)
                .WithCheck(response => Task.FromResult(
                    response.StatusCode == System.Net.HttpStatusCode.Created ||
                    response.StatusCode == System.Net.HttpStatusCode.OK));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(_config.WarmUpDuration))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 10, during: TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(copies: 30, during: TimeSpan.FromSeconds(_config.TestDuration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        // Order creation is more resource-intensive
        Assert.True(scenarioStats.Ok.Request.RPS > 25, $"Expected RPS > 25, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 500, $"Expected P95 < 500ms, got {scenarioStats.Ok.Latency.Percent95}ms");
        Assert.True(scenarioStats.Fail.Request.Count == 0, $"Expected no failures, got {scenarioStats.Fail.Request.Count}");
    }

    [Fact(Skip = "Run manually for load testing")]
    public void GetOrders_UnderLoad_ShouldMeetPerformanceTargets()
    {
        var userId = Guid.NewGuid(); // In real test, use actual user ID
        
        var scenario = Scenario.Create("get_orders_load", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_config.OrderServiceUrl}/api/orders?userId={userId}")
                .WithHeader("Accept", "application/json")
                .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(_config.WarmUpDuration))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 20, during: TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(copies: 100, during: TimeSpan.FromSeconds(_config.TestDuration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        Assert.True(scenarioStats.Ok.Request.RPS > 90, $"Expected RPS > 90, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 200, $"Expected P95 < 200ms, got {scenarioStats.Ok.Latency.Percent95}ms");
    }

    [Fact(Skip = "Run manually for load testing")]
    public void UpdateOrderStatus_UnderLoad_ShouldHandleConcurrency()
    {
        var orderId = Guid.NewGuid(); // In real test, use actual order ID
        var statuses = new[] { "Processing", "Shipped", "Delivered", "Cancelled" };
        
        var scenario = Scenario.Create("update_order_status_load", async context =>
        {
            var status = _faker.PickRandom(statuses);
            var request = Http.CreateRequest("PUT", $"{_config.OrderServiceUrl}/api/orders/{orderId}/status")
                .WithHeader("Content-Type", "application/json")
                .WithJsonBody(new { status })
                .WithCheck(response => Task.FromResult(
                    response.IsSuccessStatusCode || 
                    response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                    response.StatusCode == System.Net.HttpStatusCode.Conflict));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(_config.WarmUpDuration))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(_config.TestDuration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        // Should handle concurrent updates gracefully
        Assert.True(scenarioStats.Ok.Request.RPS > 40, $"Expected RPS > 40, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 300, $"Expected P95 < 300ms, got {scenarioStats.Ok.Latency.Percent95}ms");
    }
}
