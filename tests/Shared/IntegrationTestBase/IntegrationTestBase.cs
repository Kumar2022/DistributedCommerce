using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace IntegrationTestBase;

/// <summary>
/// Base class for integration tests with Testcontainers
/// Provides PostgreSQL, Kafka, and Redis containers for testing
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer PostgresContainer { get; private set; } = null!;
    protected KafkaContainer KafkaContainer { get; private set; } = null!;
    protected RedisContainer RedisContainer { get; private set; } = null!;

    protected string PostgresConnectionString => PostgresContainer.GetConnectionString();
    protected string KafkaBootstrapServers => KafkaContainer.GetBootstrapAddress();
    protected string RedisConnectionString => RedisContainer.GetConnectionString();

    public virtual async Task InitializeAsync()
    {
        // Initialize containers
        PostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        KafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.5.0")
            .WithCleanUp(true)
            .Build();

        RedisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .Build();

        // Start all containers in parallel
        await Task.WhenAll(
            PostgresContainer.StartAsync(),
            KafkaContainer.StartAsync(),
            RedisContainer.StartAsync()
        );
    }

    public virtual async Task DisposeAsync()
    {
        // Stop all containers in parallel
        await Task.WhenAll(
            PostgresContainer.DisposeAsync().AsTask(),
            KafkaContainer.DisposeAsync().AsTask(),
            RedisContainer.DisposeAsync().AsTask()
        );
    }
}
