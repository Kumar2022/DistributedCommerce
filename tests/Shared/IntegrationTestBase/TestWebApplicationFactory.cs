using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IntegrationTestBase;

/// <summary>
/// Generic test web application factory for integration tests
/// Replaces production dependencies with test containers
/// </summary>
/// <typeparam name="TProgram">The Program class of the service being tested</typeparam>
/// <typeparam name="TDbContext">The DbContext of the service being tested</typeparam>
public class TestWebApplicationFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>
    where TProgram : class
    where TDbContext : DbContext
{
    private readonly string _postgresConnectionString;
    private readonly string _kafkaBootstrapServers;
    private readonly string _redisConnectionString;

    public TestWebApplicationFactory(
        string postgresConnectionString,
        string kafkaBootstrapServers,
        string redisConnectionString)
    {
        _postgresConnectionString = postgresConnectionString;
        _kafkaBootstrapServers = kafkaBootstrapServers;
        _redisConnectionString = redisConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<TDbContext>));
            services.RemoveAll(typeof(TDbContext));

            // Add DbContext with test container connection string
            services.AddDbContext<TDbContext>(options =>
            {
                options.UseNpgsql(_postgresConnectionString);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Override Kafka configuration
            services.Configure<KafkaOptions>(options =>
            {
                options.BootstrapServers = _kafkaBootstrapServers;
            });

            // Override Redis configuration
            services.Configure<RedisOptions>(options =>
            {
                options.ConnectionString = _redisConnectionString;
            });

            // Build service provider and run migrations
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;

            try
            {
                var db = scopedServices.GetRequiredService<TDbContext>();
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                // Log migration errors
                Console.WriteLine($"Database migration failed: {ex.Message}");
                throw;
            }
        });

        // Disable HTTPS redirection for tests
        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// Kafka configuration options
/// </summary>
public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
}

/// <summary>
/// Redis configuration options
/// </summary>
public class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
}
