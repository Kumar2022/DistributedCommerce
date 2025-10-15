using Catalog.Application.DTOs;

namespace Catalog.Application.Queries;

// Get Category by ID
public record GetCategoryByIdQuery(Guid CategoryId) : IQuery<CategoryDto>;

public class GetCategoryByIdQueryHandler(
    ICategoryRepository repository,
    ILogger<GetCategoryByIdQueryHandler> logger)
    : IQueryHandler<GetCategoryByIdQuery, CategoryDto>
{
    public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var category = await repository.GetByIdAsync(query.CategoryId, cancellationToken);
            if (category == null)
            {
                return Result.Failure<CategoryDto>(Error.NotFound($"Category with ID {query.CategoryId} not found"));
            }

            var dto = MapToDto(category);
            return Result<CategoryDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving category {CategoryId}", query.CategoryId);
            return Result.Failure<CategoryDto>(Error.Unexpected("An error occurred while retrieving the category"));
        }
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.Slug,
            category.ParentCategoryId,
            category.ImageUrl,
            category.IsActive,
            category.DisplayOrder
        );
    }
}

// Get All Categories
public record GetAllCategoriesQuery() : IQuery<List<CategoryDto>>;

public class GetAllCategoriesQueryHandler(
    ICategoryRepository repository,
    ILogger<GetAllCategoriesQueryHandler> logger)
    : IQueryHandler<GetAllCategoriesQuery, List<CategoryDto>>
{
    public async Task<Result<List<CategoryDto>>> Handle(GetAllCategoriesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await repository.GetAllAsync(cancellationToken);
            var dtos = categories.Select(MapToDto).ToList();
            return Result<List<CategoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving categories");
            return Result.Failure<List<CategoryDto>>(Error.Unexpected("An error occurred while retrieving categories"));
        }
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.Slug,
            category.ParentCategoryId,
            category.ImageUrl,
            category.IsActive,
            category.DisplayOrder
        );
    }
}

// Get Active Categories
public record GetActiveCategoriesQuery() : IQuery<List<CategoryDto>>;

public class GetActiveCategoriesQueryHandler(
    ICategoryRepository repository,
    ILogger<GetActiveCategoriesQueryHandler> logger)
    : IQueryHandler<GetActiveCategoriesQuery, List<CategoryDto>>
{
    public async Task<Result<List<CategoryDto>>> Handle(GetActiveCategoriesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await repository.GetActiveAsync(cancellationToken);
            var dtos = categories.Select(MapToDto).ToList();
            return Result<List<CategoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active categories");
            return Result.Failure<List<CategoryDto>>(Error.Unexpected("An error occurred while retrieving active categories"));
        }
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.Slug,
            category.ParentCategoryId,
            category.ImageUrl,
            category.IsActive,
            category.DisplayOrder
        );
    }
}

// Get Root Categories (no parent)
public record GetRootCategoriesQuery() : IQuery<List<CategoryDto>>;

public class GetRootCategoriesQueryHandler(
    ICategoryRepository repository,
    ILogger<GetRootCategoriesQueryHandler> logger)
    : IQueryHandler<GetRootCategoriesQuery, List<CategoryDto>>
{
    public async Task<Result<List<CategoryDto>>> Handle(GetRootCategoriesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await repository.GetRootCategoriesAsync(cancellationToken);
            var dtos = categories.Select(MapToDto).ToList();
            return Result<List<CategoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving root categories");
            return Result.Failure<List<CategoryDto>>(Error.Unexpected("An error occurred while retrieving root categories"));
        }
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.Slug,
            category.ParentCategoryId,
            category.ImageUrl,
            category.IsActive,
            category.DisplayOrder
        );
    }
}
