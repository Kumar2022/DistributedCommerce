using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Fixtures;

namespace Catalog.IntegrationTests.API;

/// <summary>
/// Integration tests for Products API endpoints
/// Tests full HTTP request/response cycle with real database
/// </summary>
public class ProductsApiTests : IClassFixture<CatalogApiFixture>
{
    private readonly CatalogApiFixture _fixture;
    private readonly HttpClient _client;

    public ProductsApiTests(CatalogApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    [Fact]
    public async Task GetProducts_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var product = new
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = 100,
            CategoryId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", product);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdProduct = await response.Content.ReadFromJsonAsync<ProductResponse>();
        createdProduct.Should().NotBeNull();
        createdProduct!.Name.Should().Be(product.Name);
        createdProduct.Price.Should().Be(product.Price);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var product = new
        {
            Name = "", // Invalid: empty name
            Price = -10m, // Invalid: negative price
            Stock = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", product);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProduct_WithExistingId_ShouldReturnOk()
    {
        // Arrange
        var createResponse = await CreateTestProduct();
        var productId = await GetProductIdFromResponse(createResponse);

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(productId);
    }

    [Fact]
    public async Task GetProduct_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/products/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var createResponse = await CreateTestProduct();
        var productId = await GetProductIdFromResponse(createResponse);

        var updateData = new
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 149.99m,
            Stock = 50
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{productId}", updateData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify update
        var getResponse = await _client.GetAsync($"/api/products/{productId}");
        var product = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        product!.Name.Should().Be(updateData.Name);
        product.Price.Should().Be(updateData.Price);
    }

    [Fact]
    public async Task DeleteProduct_WithExistingId_ShouldReturnNoContent()
    {
        // Arrange
        var createResponse = await CreateTestProduct();
        var productId = await GetProductIdFromResponse(createResponse);

        // Act
        var response = await _client.DeleteAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchProducts_WithQuery_ShouldReturnMatchingProducts()
    {
        // Arrange
        await CreateTestProduct("Laptop", 999.99m);
        await CreateTestProduct("Desktop", 1499.99m);
        await CreateTestProduct("Mouse", 29.99m);

        // Act
        var response = await _client.GetAsync("/api/products/search?q=top");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var results = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        results.Should().NotBeNull();
        results.Should().HaveCountGreaterThanOrEqualTo(2);
        results.Should().OnlyContain(p => p.Name.Contains("top", StringComparison.OrdinalIgnoreCase));
    }

    // Helper methods
    private async Task<HttpResponseMessage> CreateTestProduct(string name = "Test Product", decimal price = 99.99m)
    {
        var product = new
        {
            Name = name,
            Description = "Test Description",
            Price = price,
            Stock = 100,
            CategoryId = Guid.NewGuid()
        };

        return await _client.PostAsJsonAsync("/api/products", product);
    }

    private async Task<Guid> GetProductIdFromResponse(HttpResponseMessage response)
    {
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        return product!.Id;
    }
}

public class ProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryId { get; set; }
}
