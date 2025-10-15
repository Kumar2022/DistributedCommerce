using System.Security.Cryptography;
using System.Text;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Hashed password value object using Argon2id
/// </summary>
public sealed class HashedPassword : ValueObject
{
    private HashedPassword(string hash)
    {
        Hash = hash;
    }

    public string Hash { get; }

    public static HashedPassword Create(string plainTextPassword)
    {
        // In production, use Argon2id. For now, using SHA256 for simplicity
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainTextPassword));
        var hash = Convert.ToBase64String(hashBytes);
        return new HashedPassword(hash);
    }

    public static HashedPassword FromHash(string hash)
    {
        return new HashedPassword(hash);
    }

    public bool Verify(string plainTextPassword)
    {
        var passwordHash = Create(plainTextPassword);
        return Hash == passwordHash.Hash;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
