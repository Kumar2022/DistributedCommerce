using Catalog.Application.Commands;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/v1/catalog/[controller]")]
[Produces("application/json")]
public class CategoriesController(
    IMediator mediator,
    ILogger<CategoriesController> logger)
    : ControllerBase
{
    private readonly ILogger<CategoriesController> _logger = logger;

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllCategories(CancellationToken cancellationToken = default)
    {
        var query = new GetAllCategoriesQuery();
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Get active categories only
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActiveCategories(CancellationToken cancellationToken = default)
    {
        var query = new GetActiveCategoriesQuery();
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Get root categories (no parent)
    /// </summary>
    [HttpGet("root")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRootCategories(CancellationToken cancellationToken = default)
    {
        var query = new GetRootCategoriesQuery();
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategoryById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCategoryByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Code == "NotFound" 
                ? NotFound(new { message = result.Error.Message })
                : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get products in a category
    /// </summary>
    [HttpGet("{id:guid}/products")]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategoryProducts(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsByCategoryQuery(id, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateCategoryCommand(
            dto.Name,
            dto.Description,
            dto.ImageUrl,
            dto.ParentCategoryId
        );

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
        }

        return CreatedAtAction(
            nameof(GetCategoryById),
            new { id = result.Value },
            new { id = result.Value });
    }

    /// <summary>
    /// Update category
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateCategoryCommand(id, dto.Name, dto.Description, dto.ImageUrl);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Code == "NotFound" 
                ? NotFound(new { message = result.Error.Message })
                : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }

    /// <summary>
    /// Activate category
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActivateCategory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new ActivateCategoryCommand(id);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Code == "NotFound" 
                ? NotFound(new { message = result.Error.Message })
                : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }

    /// <summary>
    /// Deactivate category
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateCategory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeactivateCategoryCommand(id);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Code == "NotFound" 
                ? NotFound(new { message = result.Error.Message })
                : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }
}

// DTOs for controllers
public record CreateCategoryDto(
    string Name,
    string Description,
    string? ImageUrl,
    Guid? ParentCategoryId
);

public record UpdateCategoryDto(
    string Name,
    string Description,
    string? ImageUrl
);
