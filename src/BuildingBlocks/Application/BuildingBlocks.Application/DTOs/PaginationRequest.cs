namespace BuildingBlocks.Application.DTOs;

/// <summary>
/// Base class for requests that support pagination
/// </summary>
public abstract class PaginationRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    /// <summary>
    /// The page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// The number of items per page (max 100)
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    /// <summary>
    /// Calculate the number of items to skip
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// The number of items to take
    /// </summary>
    public int Take => PageSize;
}
