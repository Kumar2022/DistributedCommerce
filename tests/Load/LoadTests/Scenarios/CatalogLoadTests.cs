using LoadTests.Config;

namespace LoadTests.Scenarios;

/// <summary>
/// Load tests for Catalog Service
/// Tests product browsing, searching, and retrieval under load
/// </summary>
public class CatalogLoadTests
{
    private readonly LoadTestConfig _config = LoadTestConfig.FromEnvironment();
    private readonly Faker _faker = new();

    [Fact(Skip = "Run manually for load testing")]
    public void GetProducts_UnderNormalLoad_ShouldMeetPerformanceTargets()
    {
        var scenario = Scenario.Create("get_products_normal_load", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_config.CatalogServiceUrl}/api/products")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(request, context);
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(_config.WarmUpDuration))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 10, during: TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(_config.TestDuration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert performance targets
        var scenarioStats = stats.ScenarioStats[0];
        Assert.True(scenarioStats.Ok.Request.RPS > 90, $"Expected RPS > 90, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 200, $"Expected P95 < 200ms, got {scenarioStats.Ok.Latency.Percent95}ms");
        Assert.True(scenarioStats.Fail.Request.Count == 0, $"Expected no failures, got {scenarioStats.Fail.Request.Count}");
    }

    [Fact(Skip = "Run manually for load testing")]
    public void GetProducts_UnderHighLoad_ShouldHandleGracefully()
    {
        var scenario = Scenario.Create("get_products_high_load", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_config.CatalogServiceUrl}/api/products")
                .WithHeader("Accept", "application/json")
                .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(_config.WarmUpDuration))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(copies: 200, during: TimeSpan.FromSeconds(_config.TestDuration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        // Under high load, we expect some degradation but still acceptable performance
        Assert.True(scenarioStats.Ok.Request.RPS > 150, $"Expected RPS > 150, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 500, $"Expected P95 < 500ms, got {scenarioStats.Ok.Latency.Percent95}ms");
        
        // Allow up to 1% error rate under high load
        var errorRate = (double)scenarioStats.Fail.Request.Count / scenarioStats.AllRequestCount * 100;
        Assert.True(errorRate < 1.0, $"Expected error rate < 1%, got {errorRate:F2}%");
    }

    [Fact(Skip = "Run manually for spike testing")]
    public void GetProducts_UnderSpikeLoad_ShouldRecoverQuickly()
    {
        var scenario = Scenario.Create("get_products_spike", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_config.CatalogServiceUrl}/api/products")
                .WithHeader("Accept", "application/json")
                .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(copies: 500, during: TimeSpan.FromSeconds(5)), // Spike!
            Simulation.KeepConstant(copies: 500, during: TimeSpan.FromSeconds(20)),
            Simulation.RampingConstant(copies: 10, during: TimeSpan.FromSeconds(5))  // Recovery
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        // System should handle spike with degradation but no crashes
        Assert.True(scenarioStats.Ok.Request.RPS > 100, $"Expected RPS > 100 during spike, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent99 < 2000, $"Expected P99 < 2s during spike, got {scenarioStats.Ok.Latency.Percent99}ms");
        
        // Allow higher error rate during spike (up to 5%)
        var errorRate = (double)scenarioStats.Fail.Request.Count / scenarioStats.AllRequestCount * 100;
        Assert.True(errorRate < 5.0, $"Expected error rate < 5% during spike, got {errorRate:F2}%");
    }

    [Fact(Skip = "Run manually for load testing")]
    public void SearchProducts_UnderLoad_ShouldMeetPerformanceTargets()
    {
        var searchTerms = new[] { "laptop", "phone", "headphones", "keyboard", "mouse", "monitor" };
        
        var scenario = Scenario.Create("search_products_load", async context =>
        {
            var searchTerm = _faker.PickRandom(searchTerms);
            var request = Http.CreateRequest("GET", $"{_config.CatalogServiceUrl}/api/products/search?query={searchTerm}")
                .WithHeader("Accept", "application/json")
                .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(_config.WarmUpDuration))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 10, during: TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(_config.TestDuration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        // Search should be fast even under load
        Assert.True(scenarioStats.Ok.Request.RPS > 80, $"Expected RPS > 80, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 300, $"Expected P95 < 300ms for search, got {scenarioStats.Ok.Latency.Percent95}ms");
    }

    [Fact(Skip = "Run manually for load testing")]
    public void GetProductById_CachingEffect_ShouldShowImprovement()
    {
        var productId = Guid.NewGuid().ToString(); // In real test, use actual product ID
        
        var scenario = Scenario.Create("get_product_by_id_caching", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_config.CatalogServiceUrl}/api/products/{productId}")
                .WithHeader("Accept", "application/json")
                .WithCheck(response => Task.FromResult(response.StatusCode == System.Net.HttpStatusCode.OK || 
                                                       response.StatusCode == System.Net.HttpStatusCode.NotFound));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(_config.WarmUpDuration))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 100, during: TimeSpan.FromSeconds(_config.TestDuration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        // With caching, should handle very high RPS with low latency
        Assert.True(scenarioStats.Ok.Request.RPS > 200, $"Expected RPS > 200 (cached), got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 50, $"Expected P95 < 50ms (cached), got {scenarioStats.Ok.Latency.Percent95}ms");
    }

    [Fact(Skip = "Run manually for stress testing")]
    public void GetProducts_StressTest_FindBreakingPoint()
    {
        var scenario = Scenario.Create("get_products_stress", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_config.CatalogServiceUrl}/api/products")
                .WithHeader("Accept", "application/json")
                .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 100, during: TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(copies: 200, during: TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(copies: 400, during: TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(copies: 600, during: TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(copies: 800, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        // Document where system starts to degrade
        // This test is more for observation than assertion
        Assert.True(scenarioStats.Ok.Request.Count > 0, "System should handle some requests even under extreme stress");
    }
}
