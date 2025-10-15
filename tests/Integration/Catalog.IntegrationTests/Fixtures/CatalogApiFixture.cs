using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Catalog.IntegrationTests.Fixtures;

/// <summary>
/// Integration test fixture using TestContainers for PostgreSQL
/// Provides a real database instance for testing
/// </summary>
public class CatalogApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString => _postgresContainer.GetConnectionString();

    public CatalogApiFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("catalogdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        await _postgresContainer.StartAsync();

        // Create test factory
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add DbContext with TestContainer connection
                    services.AddDbContext<CatalogDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });

                    // Build service provider and run migrations
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
                    db.Database.Migrate();
                });
            });

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        _factory?.Dispose();
        Client?.Dispose();
    }
}

// Placeholder DbContext (replace with actual)
public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
}

// Placeholder Program class
public class Program { }
