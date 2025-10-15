using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Analytics.Application.Queries;
using Analytics.Application.DTOs;
using BuildingBlocks.Authentication.Services;

namespace Analytics.API.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Produces("application/json")]
[Authorize(Roles = "Admin,Analyst")]
public class AnalyticsController(
    IMediator mediator,
    ILogger<AnalyticsController> logger,
    ICurrentUserService currentUser)
    : ControllerBase
{
    /// <summary>
    /// Get order summary for a date range
    /// </summary>
    [HttpGet("orders/summary")]
    [ProducesResponseType(typeof(OrderMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrderSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        logger.LogInformation("User {UserId} requesting order summary from {StartDate} to {EndDate}", 
            currentUser.UserId, startDate, endDate);
        var query = new GetOrderSummaryQuery(startDate, endDate);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get daily revenue for a specific date
    /// </summary>
    [HttpGet("revenue/daily")]
    [ProducesResponseType(typeof(RevenueMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDailyRevenue([FromQuery] DateTime date)
    {
        var query = new GetDailyRevenueQuery(date);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    /// <summary>
    /// Get monthly revenue breakdown
    /// </summary>
    [HttpGet("revenue/monthly")]
    [ProducesResponseType(typeof(List<RevenueMetricsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlyRevenue(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        if (year < 2000 || year > DateTime.UtcNow.Year + 1)
            return BadRequest("Invalid year");

        if (month < 1 || month > 12)
            return BadRequest("Invalid month");

        var query = new GetMonthlyRevenueQuery(year, month);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get top selling products for a date range
    /// </summary>
    [HttpGet("products/top-selling")]
    [ProducesResponseType(typeof(List<ProductMetricsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopSellingProducts(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int count = 10)
    {
        if (count < 1 || count > 100)
            return BadRequest("Count must be between 1 and 100");

        var query = new GetTopSellingProductsQuery(startDate, endDate, count);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get customer lifetime value
    /// </summary>
    [HttpGet("customers/{customerId}/lifetime-value")]
    [ProducesResponseType(typeof(CustomerMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerLifetimeValue(Guid customerId)
    {
        var query = new GetCustomerLifetimeValueQuery(customerId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    /// <summary>
    /// Get conversion funnel metrics
    /// </summary>
    [HttpGet("conversion-funnel")]
    [ProducesResponseType(typeof(ConversionFunnelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConversionFunnel(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var query = new GetConversionFunnelQuery(startDate, endDate);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get real-time dashboard summary
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var query = new GetDashboardSummaryQuery();
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(500, result.Error);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
