using BuildingBlocks.Saga.Abstractions;
using BuildingBlocks.Saga.Orchestration;
using BuildingBlocks.Saga.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Saga.Extensions;

/// <summary>
/// Extension methods for adding Saga support to services
/// </summary>
public static class SagaServiceCollectionExtensions
{
    /// <summary>
    /// Add saga orchestration support with in-memory storage (for development/testing)
    /// </summary>
    public static IServiceCollection AddSagaOrchestration(this IServiceCollection services)
    {
        // Register saga orchestrator as scoped (one per request)
        services.AddScoped(typeof(SagaOrchestrator<>));
        
        // Register in-memory saga state repository (can be overridden for persistence)
        services.AddSingleton(typeof(ISagaStateRepository<>), typeof(InMemorySagaStateRepository<>));
        
        return services;
    }
    
    /// <summary>
    /// Add saga orchestration with PostgreSQL persistence (for production)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSagaOrchestrationWithPostgres(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        // Register DbContext for saga state
        services.AddDbContext<SagaStateDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        // Register saga orchestrator as scoped
        services.AddScoped(typeof(SagaOrchestrator<>));
        
        // Register PostgreSQL saga state repository
        services.AddScoped(typeof(ISagaStateRepository<>), typeof(PostgresSagaStateRepository<>));
        
        return services;
    }
    
    /// <summary>
    /// Add saga orchestration with custom repository
    /// </summary>
    public static IServiceCollection AddSagaOrchestration<TRepository>(
        this IServiceCollection services) 
        where TRepository : class
    {
        services.AddScoped(typeof(SagaOrchestrator<>));
        services.AddScoped(typeof(ISagaStateRepository<>), typeof(TRepository));
        
        return services;
    }
}
