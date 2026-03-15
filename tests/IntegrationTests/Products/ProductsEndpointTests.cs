using CleanArchitecture.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace CleanArchitecture.IntegrationTests.Products;

public sealed class ProductsEndpointTests(WebApplicationFactoryFixture factory)
    : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ShouldReturn200OK()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithValidCommand_ShouldReturn201Created()
    {
        // Arrange
        var command = new { Name = "Integration Test Product", Description = "Test", Price = 49.99m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Integration Test Product");
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithInvalidData_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var command = new { Name = "", Price = -1m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
