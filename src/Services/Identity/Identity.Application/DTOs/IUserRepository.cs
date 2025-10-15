using Identity.Domain.Aggregates.UserAggregate;

namespace Identity.Application.DTOs;

/// <summary>
/// Repository interface for User aggregate
/// </summary>
public interface IUserRepository : IRepository<User, Guid>
{
    /// <summary>
    /// Get user by email address
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
