namespace BuildingBlocks.Application.DTOs;

/// <summary>
/// Represents a paged collection of items with pagination metadata
/// </summary>
/// <typeparam name="T">The type of items in the collection</typeparam>
public sealed class PagedResult<T>(
    IReadOnlyList<T> items,
    int page,
    int pageSize,
    int totalCount)
{
    /// <summary>
    /// The items in the current page
    /// </summary>
    public IReadOnlyList<T> Items { get; } = items;

    /// <summary>
    /// The current page number (1-based)
    /// </summary>
    public int Page { get; } = page;

    /// <summary>
    /// The number of items per page
    /// </summary>
    public int PageSize { get; } = pageSize;

    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    public int TotalCount { get; } = totalCount;

    /// <summary>
    /// The total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Create an empty paged result
    /// </summary>
    public static PagedResult<T> Empty() => new([], 1, 10, 0);

    /// <summary>
    /// Create a paged result from a full collection
    /// </summary>
    public static PagedResult<T> Create(
        IReadOnlyList<T> source,
        int page,
        int pageSize)
    {
        var items = source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>(items, page, pageSize, source.Count);
    }
}
