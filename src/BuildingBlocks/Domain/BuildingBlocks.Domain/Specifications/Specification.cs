namespace BuildingBlocks.Domain.Specifications;

/// <summary>
/// Specification pattern for encapsulating business rules and query criteria
/// </summary>
/// <typeparam name="T">The type being specified</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Check if the given entity satisfies the specification
    /// </summary>
    bool IsSatisfiedBy(T entity);
}

/// <summary>
/// Base class for specifications with composability support
/// </summary>
/// <typeparam name="T">The type being specified</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    public abstract bool IsSatisfiedBy(T entity);

    public Specification<T> And(Specification<T> other)
        => new AndSpecification<T>(this, other);

    public Specification<T> Or(Specification<T> other)
        => new OrSpecification<T>(this, other);

    public Specification<T> Not()
        => new NotSpecification<T>(this);
}

public sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T entity)
        => left.IsSatisfiedBy(entity) && right.IsSatisfiedBy(entity);
}

public sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T entity)
        => left.IsSatisfiedBy(entity) || right.IsSatisfiedBy(entity);
}

public sealed class NotSpecification<T>(Specification<T> specification) : Specification<T>
{
    public override bool IsSatisfiedBy(T entity)
        => !specification.IsSatisfiedBy(entity);
}
