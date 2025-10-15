using Catalog.Application.Commands;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Authentication.Services;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/v1/catalog/[controller]")]
[Produces("application/json")]
public class ProductsController(
    IMediator mediator,
    ILogger<ProductsController> logger,
    ICurrentUserService currentUser)
    : ControllerBase
{
    private readonly ILogger<ProductsController> _logger = logger;
    private readonly ICurrentUserService _currentUser = currentUser;

    /// <summary>
    /// Search products with optional filtering
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchProductsQuery(searchTerm, categoryId, minPrice, maxPrice, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductByIdQuery(id);
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
    /// Get product by SKU
    /// </summary>
    [HttpGet("sku/{sku}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductBySku(
        string sku,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductBySkuQuery(sku);
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
    /// Get featured products
    /// </summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFeaturedProducts(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFeaturedProductsQuery(count);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{categoryId:guid}")]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductsByCategory(
        Guid categoryId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsByCategoryQuery(categoryId, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateProductCommand(
            dto.Name,
            dto.Description,
            dto.Sku,
            dto.CategoryId,
            dto.Brand,
            dto.Price,
            dto.Currency
        );

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Code == "Conflict" 
                ? Conflict(new { message = result.Error.Message })
                : Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
        }

        return CreatedAtAction(
            nameof(GetProductById),
            new { id = result.Value },
            new { id = result.Value });
    }

    /// <summary>
    /// Update product details
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateProductCommand(id, dto.Name, dto.Description, dto.Brand);
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
    /// Update product price
    /// </summary>
    [HttpPut("{id:guid}/price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePrice(
        Guid id,
        [FromBody] UpdatePriceDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdatePriceCommand(id, dto.Price, dto.CompareAtPrice);
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
    /// Publish a product
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PublishProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new PublishProductCommand(id);
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
    /// Unpublish a product
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnpublishProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new UnpublishProductCommand(id);
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
    /// Add image to product
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddImage(
        Guid id,
        [FromBody] AddImageDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new AddImageCommand(id, dto.Url, dto.AltText, dto.IsPrimary);
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
    /// Add attribute to product
    /// </summary>
    [HttpPost("{id:guid}/attributes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddAttribute(
        Guid id,
        [FromBody] AddAttributeDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new AddAttributeCommand(id, dto.Key, dto.Value, dto.DisplayName);
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
    /// Set product as featured
    /// </summary>
    [HttpPost("{id:guid}/featured")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetFeatured(
        Guid id,
        [FromQuery] bool isFeatured = true,
        CancellationToken cancellationToken = default)
    {
        var command = new SetFeaturedCommand(id, isFeatured);
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
