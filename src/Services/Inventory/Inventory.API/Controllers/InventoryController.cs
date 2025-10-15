using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.Commands;
using Inventory.Application.DTOs;
using Inventory.Application.Queries;
using BuildingBlocks.Authentication.Services;

namespace Inventory.API.Controllers;

/// <summary>
/// Handles inventory and stock management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InventoryController> _logger;
    private readonly IInventoryQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public InventoryController(
        IMediator mediator,
        ILogger<InventoryController> logger,
        IInventoryQueryService queryService,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _logger = logger;
        _queryService = queryService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Create a new product in inventory
    /// </summary>
    /// <param name="command">Product creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product ID</returns>
    [HttpPost("products")]
    [Authorize(Roles = "Admin,InventoryManager")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} ({Email}) creating product with SKU: {Sku}", 
            _currentUser.UserId, _currentUser.Email, command.Sku);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Product creation failed for SKU {Sku}: {Error}",
                command.Sku, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Product Creation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Product created successfully with ID: {ProductId}", result.Value);
        return CreatedAtAction(
            nameof(GetProductById),
            new { id = result.Value },
            new { productId = result.Value });
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details</returns>
    [HttpGet("products/{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving product: {ProductId}", id);

        var product = await _queryService.GetProductByIdAsync(id, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            return NotFound(new ProblemDetails
            {
                Title = "Product Not Found",
                Detail = $"Product with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(product);
    }

    /// <summary>
    /// Get all products with pagination
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products</returns>
    [HttpGet("products")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving products - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        // For now, get all low stock products as a simple list
        // TODO: Implement proper pagination in the query service
        var products = await _queryService.GetLowStockProductsAsync(int.MaxValue, cancellationToken);

        return Ok(products);
    }

    /// <summary>
    /// Get products with low stock levels
    /// </summary>
    /// <param name="threshold">Stock threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of low stock products</returns>
    [HttpGet("products/low-stock")]
    [Authorize(Roles = "Admin,InventoryManager")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLowStockProducts(
        [FromQuery] int threshold = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} retrieving low stock products with threshold: {Threshold}", 
            _currentUser.UserId, threshold);

        var products = await _queryService.GetLowStockProductsAsync(threshold, cancellationToken);

        return Ok(products);
    }

    /// <summary>
    /// Reserve stock for an order
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Reservation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("products/{productId:guid}/reserve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReserveStock(
        Guid productId,
        [FromBody] ReserveStockRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} reserving {Quantity} units of product {ProductId} for order {OrderId}",
            _currentUser.UserId, request.Quantity, productId, request.OrderId);

        var command = new ReserveStockCommand(productId, request.OrderId, request.Quantity);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Stock reservation failed for product {ProductId}: {Error}",
                productId, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Stock Reservation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Stock reserved successfully for product {ProductId}", productId);
        return Ok(new { message = "Stock reserved successfully" });
    }

    /// <summary>
    /// Confirm a stock reservation
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Confirmation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("products/{productId:guid}/confirm-reservation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmReservation(
        Guid productId,
        [FromBody] ConfirmReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Confirming reservation {ReservationId} for product {ProductId}",
            request.ReservationId, productId);

        var command = new ConfirmReservationCommand(productId, request.ReservationId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Reservation confirmation failed for product {ProductId}: {Error}",
                productId, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Reservation Confirmation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Reservation confirmed successfully: {ReservationId}", request.ReservationId);
        return Ok(new { message = "Reservation confirmed successfully" });
    }

    /// <summary>
    /// Release a stock reservation
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Release details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("products/{productId:guid}/release")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReleaseReservation(
        Guid productId,
        [FromBody] ReleaseReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Releasing reservation for product {ProductId}, order {OrderId}",
            productId, request.OrderId);

        var command = new ReleaseReservationCommand(productId, request.OrderId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Reservation release failed for product {ProductId}: {Error}",
                productId, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Reservation Release Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Reservation released successfully for product {ProductId}", productId);
        return Ok(new { message = "Reservation released successfully" });
    }

    /// <summary>
    /// Adjust stock level for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Adjustment details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("products/{productId:guid}/adjust-stock")]
    [Authorize(Roles = "Admin,InventoryManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdjustStock(
        Guid productId,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} adjusting stock for product {ProductId} by {Quantity}. Reason: {Reason}",
            _currentUser.UserId, productId, request.Quantity, request.Reason);

        var command = new AdjustStockCommand(productId, request.Quantity, request.Reason);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Stock adjustment failed for product {ProductId}: {Error}",
                productId, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Stock Adjustment Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = result.Error.Code
            });
        }

        _logger.LogInformation("Stock adjusted successfully for product: {ProductId}", productId);
        return Ok(new { message = "Stock adjusted successfully" });
    }

    /// <summary>
    /// Get reservations for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reservations</returns>
    [HttpGet("products/{productId:guid}/reservations")]
    [ProducesResponseType(typeof(IEnumerable<StockReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetProductReservations(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving reservations for product: {ProductId}", productId);

        // TODO: Implement GetProductReservationsAsync in query service
        // For now, return empty list
        var reservations = new List<StockReservationDto>();

        return Ok(reservations);
    }
}

/// <summary>
/// Request DTO for reserving stock
/// </summary>
public record ReserveStockRequest(Guid OrderId, int Quantity);

/// <summary>
/// Request DTO for confirming reservation
/// </summary>
public record ConfirmReservationRequest(Guid ReservationId);

/// <summary>
/// Request DTO for releasing reservation
/// </summary>
public record ReleaseReservationRequest(Guid OrderId);

/// <summary>
/// Request DTO for adjusting stock
/// </summary>
public record AdjustStockRequest(int Quantity, string Reason);
