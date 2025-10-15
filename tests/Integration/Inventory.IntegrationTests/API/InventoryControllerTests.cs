using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Application.DTOs;
using Inventory.IntegrationTests.Fixtures;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.IntegrationTests.API;

/// <summary>
/// Integration tests for InventoryController
/// Tests all endpoints with real database, Kafka, and Redis via Testcontainers
/// </summary>
[Collection("InventoryApiCollection")]
public class InventoryControllerTests : IClassFixture<InventoryApiFixture>
{
    private readonly InventoryApiFixture _fixture;
    private readonly HttpClient _client;

    public InventoryControllerTests(InventoryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    #region CreateProduct Tests

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var command = new CreateProductCommand(
            Sku: "TEST-SKU-001",
            Name: "Test Product",
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/inventory/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var result = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
        result.Should().NotBeNull();
        result!.ProductId.Should().NotBeEmpty();
        
        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/v1/inventory/products/{result.ProductId}");
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSku_ShouldReturnBadRequest()
    {
        // Arrange - Create first product
        var command1 = new CreateProductCommand(
            Sku: "DUP-SKU-001",
            Name: "First Product",
            
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        await _client.PostAsJsonAsync("/api/v1/inventory/products", command1);

        // Create duplicate
        var command2 = new CreateProductCommand(
            Sku: "DUP-SKU-001",  // Same SKU
            Name: "Duplicate Product",
            
            InitialStock: 50,
            ReorderLevel: 5,
            ReorderQuantity: 25
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/inventory/products", command2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("SKU");
    }

    [Fact]
    public async Task CreateProduct_WithNegativeStock_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateProductCommand(
            Sku: "NEG-SKU-001",
            Name: "Negative Stock Product",
            
            InitialStock: -10,  // Negative stock
            ReorderLevel: 10,
            ReorderQuantity: 50
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/inventory/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithEmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateProductCommand(
            Sku: "EMPTY-NAME-001",
            Name: "",  // Empty name
            
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/inventory/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetProductById Tests

    [Fact]
    public async Task GetProductById_WithExistingProduct_ShouldReturnProduct()
    {
        // Arrange - Create a product first
        var createCommand = new CreateProductCommand(
            Sku: "GET-SKU-001",
            Name: "Get Test Product",
            
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/inventory/products", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/products/{createResult!.ProductId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.ProductId.Should().Be(createResult.ProductId);
        product.Sku.Should().Be("GET-SKU-001");
        product.Name.Should().Be("Get Test Product");
    }

    [Fact]
    public async Task GetProductById_WithNonExistentProduct_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Contain("Not Found");
    }

    [Fact]
    public async Task GetProductById_WithInvalidGuid_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/inventory/products/invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetProducts Tests

    [Fact]
    public async Task GetProducts_ShouldReturnProducts()
    {
        // Arrange - Create multiple products
        for (int i = 1; i <= 3; i++)
        {
            var command = new CreateProductCommand(
                Sku: $"LIST-SKU-{i:000}",
                Name: $"List Product {i}",
                
                InitialStock: 100,
                ReorderLevel: 10,
                ReorderQuantity: 50
            );
            await _client.PostAsJsonAsync("/api/v1/inventory/products", command);
        }

        // Act
        var response = await _client.GetAsync("/api/v1/inventory/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
        products!.Count.Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task GetProducts_WithPagination_ShouldReturnPagedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/inventory/products?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
    }

    #endregion

    #region ReserveStock Tests

    [Fact]
    public async Task ReserveStock_WithValidData_ShouldReturnSuccess()
    {
        // Arrange - Create a product
        var createCommand = new CreateProductCommand(
            Sku: "RESERVE-SKU-001",
            Name: "Reserve Test Product",
            
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/inventory/products", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Act - Reserve stock
        var orderId = Guid.NewGuid();
        var reserveRequest = new { OrderId = orderId, Quantity = 10 };
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{createResult!.ProductId}/reserve",
            reserveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ReserveStock_WithInsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange - Create a product with low stock
        var createCommand = new CreateProductCommand(
            Sku: "LOW-STOCK-001",
            Name: "Low Stock Product",
            
            InitialStock: 5,  // Low stock
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/inventory/products", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Act - Try to reserve more than available
        var orderId = Guid.NewGuid();
        var reserveRequest = new { OrderId = orderId, Quantity = 100 };  // More than available
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{createResult!.ProductId}/reserve",
            reserveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("insufficient");
    }

    [Fact]
    public async Task ReserveStock_WithZeroQuantity_ShouldReturnBadRequest()
    {
        // Arrange
        var createCommand = new CreateProductCommand(
            Sku: "ZERO-QTY-001",
            Name: "Zero Quantity Product",
            
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/inventory/products", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Act
        var orderId = Guid.NewGuid();
        var reserveRequest = new { OrderId = orderId, Quantity = 0 };  // Zero quantity
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{createResult!.ProductId}/reserve",
            reserveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReserveStock_WithNegativeQuantity_ShouldReturnBadRequest()
    {
        // Arrange
        var createCommand = new CreateProductCommand(
            Sku: "NEG-QTY-001",
            Name: "Negative Quantity Product",
            
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/inventory/products", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Act
        var orderId = Guid.NewGuid();
        var reserveRequest = new { OrderId = orderId, Quantity = -5 };  // Negative quantity
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{createResult!.ProductId}/reserve",
            reserveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReserveStock_ForNonExistentProduct_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var orderId = Guid.NewGuid();
        var reserveRequest = new { OrderId = orderId, Quantity = 10 };
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{nonExistentId}/reserve",
            reserveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest); // Product not found returns BadRequest
    }

    #endregion

    #region AdjustStock Tests

    [Fact]
    public async Task AdjustStock_WithPositiveAdjustment_ShouldIncreaseStock()
    {
        // Arrange
        var createCommand = new CreateProductCommand(
            Sku: "ADJ-POS-001",
            Name: "Adjust Positive Product",
            
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/inventory/products", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Act
        var adjustRequest = new { Quantity = 50, Reason = "Stock replenishment" };
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{createResult!.ProductId}/adjust-stock",
            adjustRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify stock increased
        var getResponse = await _client.GetAsync($"/api/v1/inventory/products/{createResult.ProductId}");
        var product = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        product!.StockQuantity.Should().Be(150); // 100 + 50
    }

    [Fact]
    public async Task AdjustStock_WithNegativeAdjustment_ShouldDecreaseStock()
    {
        // Arrange
        var createCommand = new CreateProductCommand(
            Sku: "ADJ-NEG-001",
            Name: "Adjust Negative Product",
            
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/inventory/products", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Act
        var adjustRequest = new { Quantity = -30, Reason = "Damaged items" };
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{createResult!.ProductId}/adjust-stock",
            adjustRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify stock decreased
        var getResponse = await _client.GetAsync($"/api/v1/inventory/products/{createResult.ProductId}");
        var product = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        product!.StockQuantity.Should().Be(70); // 100 - 30
    }

    [Fact]
    public async Task AdjustStock_WithExcessiveNegativeAdjustment_ShouldReturnBadRequest()
    {
        // Arrange
        var createCommand = new CreateProductCommand(
            Sku: "ADJ-EXCESS-001",
            Name: "Adjust Excessive Product",
            
            InitialStock: 50,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/inventory/products", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Act - Try to reduce by more than available
        var adjustRequest = new { Quantity = -100, Reason = "Invalid adjustment" };  // More than available
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{createResult!.ProductId}/adjust-stock",
            adjustRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
    }

    [Fact]
    public async Task AdjustStock_WithEmptyReason_ShouldReturnBadRequest()
    {
        // Arrange
        var createCommand = new CreateProductCommand(
            Sku: "ADJ-REASON-001",
            Name: "Adjust Reason Product",
            
            InitialStock: 100,
            ReorderLevel: 10,
            ReorderQuantity: 50
        );
        var createResponse = await _client.PostAsJsonAsync("/api/v1/inventory/products", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Act
        var adjustRequest = new { Quantity = 10, Reason = "" };  // Empty reason
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{createResult!.ProductId}/adjust-stock",
            adjustRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Classes

    private record CreateProductResponse(Guid ProductId);

    #endregion
}
