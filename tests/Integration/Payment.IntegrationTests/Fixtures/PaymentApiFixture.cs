using IntegrationTestBase;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Persistence;

namespace Payment.IntegrationTests.Fixtures;

/// <summary>
/// Test fixture for Payment API integration tests
/// Manages Testcontainers lifecycle and provides HttpClient for testing
/// </summary>
public class PaymentApiFixture : IntegrationTestBase.IntegrationTestBase, IAsyncLifetime
{
    private TestWebApplicationFactory<Program, PaymentDbContext>? _factory;
    
    public HttpClient Client { get; private set; } = null!;
    public PaymentDbContext DbContext { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        // Initialize base containers (PostgreSQL, Kafka, Redis)
        await base.InitializeAsync();

        // Create test web application factory with test containers
        _factory = new TestWebApplicationFactory<Program, PaymentDbContext>(
            PostgresConnectionString,
            KafkaBootstrapServers,
            RedisConnectionString);

        // Create HTTP client for API testing
        Client = _factory.CreateClient();

        // Get DbContext for direct database access in tests
        var scope = _factory.Services.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    }

    public override async Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        await base.DisposeAsync();
    }
    
    /// <summary>
    /// Clean up database between tests
    /// </summary>
    public async Task ResetDatabase()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
    }
}
