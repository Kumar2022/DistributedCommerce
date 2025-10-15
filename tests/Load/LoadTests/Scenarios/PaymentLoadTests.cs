using LoadTests.Config;

namespace LoadTests.Scenarios;

/// <summary>
/// Load tests for Payment Service
/// Tests payment processing under load with realistic transaction volumes
/// </summary>
public class PaymentLoadTests
{
    private readonly LoadTestConfig _config = LoadTestConfig.FromEnvironment();
    private readonly Faker _faker = new();

    [Fact(Skip = "Run manually for load testing")]
    public void ProcessPayment_UnderLoad_ShouldMeetPerformanceTargets()
    {
        var scenario = Scenario.Create("process_payment_load", async context =>
        {
            var paymentData = new
            {
                orderId = Guid.NewGuid(),
                amount = _faker.Random.Decimal(10, 1000),
                currency = "USD",
                paymentMethod = _faker.PickRandom(new[] { "CreditCard", "DebitCard", "PayPal", "Stripe" }),
                cardNumber = _faker.Finance.CreditCardNumber(),
                cardHolderName = _faker.Name.FullName(),
                expiryDate = $"{_faker.Random.Int(1, 12):D2}/{_faker.Random.Int(24, 30):D2}",
                cvv = _faker.Random.Int(100, 999).ToString()
            };

            var request = Http.CreateRequest("POST", $"{_config.PaymentServiceUrl}/api/payments")
                .WithHeader("Content-Type", "application/json")
                .WithJsonBody(paymentData)
                .WithCheck(response => Task.FromResult(
                    response.StatusCode == System.Net.HttpStatusCode.OK ||
                    response.StatusCode == System.Net.HttpStatusCode.Created ||
                    response.StatusCode == System.Net.HttpStatusCode.Accepted));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(_config.WarmUpDuration))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 5, during: TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(_config.TestDuration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        // Payment processing is critical - must be reliable
        Assert.True(scenarioStats.Ok.Request.RPS > 15, $"Expected RPS > 15, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 1000, $"Expected P95 < 1s, got {scenarioStats.Ok.Latency.Percent95}ms");
        Assert.True(scenarioStats.Fail.Request.Count == 0, $"Expected no failures in payment, got {scenarioStats.Fail.Request.Count}");
    }

    [Fact(Skip = "Run manually for load testing")]
    public void GetPaymentStatus_UnderLoad_ShouldBeFast()
    {
        var paymentId = Guid.NewGuid(); // In real test, use actual payment ID
        
        var scenario = Scenario.Create("get_payment_status_load", async context =>
        {
            var request = Http.CreateRequest("GET", $"{_config.PaymentServiceUrl}/api/payments/{paymentId}")
                .WithHeader("Accept", "application/json")
                .WithCheck(response => Task.FromResult(
                    response.IsSuccessStatusCode || 
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
        
        // Status checks should be very fast (likely cached)
        Assert.True(scenarioStats.Ok.Request.RPS > 180, $"Expected RPS > 180, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 100, $"Expected P95 < 100ms, got {scenarioStats.Ok.Latency.Percent95}ms");
    }

    [Fact(Skip = "Run manually for load testing")]
    public void RefundPayment_UnderLoad_ShouldHandleCarefully()
    {
        var paymentId = Guid.NewGuid(); // In real test, use actual payment ID
        
        var scenario = Scenario.Create("refund_payment_load", async context =>
        {
            var refundData = new
            {
                paymentId,
                amount = _faker.Random.Decimal(10, 100),
                reason = _faker.PickRandom(new[] { "Customer Request", "Defective Product", "Order Cancelled" })
            };

            var request = Http.CreateRequest("POST", $"{_config.PaymentServiceUrl}/api/payments/{paymentId}/refund")
                .WithHeader("Content-Type", "application/json")
                .WithJsonBody(refundData)
                .WithCheck(response => Task.FromResult(
                    response.IsSuccessStatusCode || 
                    response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                    response.StatusCode == System.Net.HttpStatusCode.Conflict));

            return await Http.Send(request, context);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(_config.WarmUpDuration))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(_config.TestDuration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        
        // Refunds are less frequent but must be reliable
        Assert.True(scenarioStats.Ok.Request.RPS > 8, $"Expected RPS > 8, got {scenarioStats.Ok.Request.RPS}");
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 800, $"Expected P95 < 800ms, got {scenarioStats.Ok.Latency.Percent95}ms");
    }
}
