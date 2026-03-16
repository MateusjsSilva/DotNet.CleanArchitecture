using CleanArchitecture.Application.DTOs;
using CleanArchitecture.WebAPI.Models;
using System.Net;
using System.Net.Http.Json;

namespace CleanArchitecture.IntegrationTests.Products;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProductsMutationTests(WebApplicationFactoryFixture factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Update_WhenProductExists_ShouldReturn200WithUpdatedData()
    {
        // Arrange — create a product first
        var created = await CreateProductAsync("Original Name", 10m);

        var updatePayload = new { Name = "Updated Name", Description = "Updated Desc", Price = 99.99m };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/products/{created.Id}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body!.Data.Name.Should().Be("Updated Name");
        body.Data.Price.Should().Be(99.99m);
    }

    [Fact]
    public async Task Update_WithNonExistentId_ShouldReturn404()
    {
        var payload = new { Name = "Name", Price = 10m };
        var response = await _client.PutAsJsonAsync($"/api/v1/products/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithInvalidData_ShouldReturn422()
    {
        var created = await CreateProductAsync("Original", 10m);
        var payload = new { Name = "", Price = -1m };

        var response = await _client.PutAsJsonAsync($"/api/v1/products/{created.Id}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Delete_WhenProductExists_ShouldReturn204()
    {
        // Arrange
        var created = await CreateProductAsync("To Delete", 10m);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/products/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WhenProductExists_ProductShouldBeHiddenAfterDeletion()
    {
        // Arrange
        var created = await CreateProductAsync("To Soft Delete", 10m);

        // Act
        await _client.DeleteAsync($"/api/v1/products/{created.Id}");

        // Assert — GetById should return 404 (soft delete applies query filter)
        var getResponse = await _client.GetAsync($"/api/v1/products/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ShouldReturn404()
    {
        var response = await _client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_AfterCacheHit_ShouldReturnFreshData()
    {
        // Arrange — create and prime cache with first GET
        var created = await CreateProductAsync("Cached Name", 10m);
        await _client.GetAsync($"/api/v1/products/{created.Id}"); // prime cache

        // Act — update (invalidates cache)
        var updatePayload = new { Name = "Fresh Name", Price = 50m };
        await _client.PutAsJsonAsync($"/api/v1/products/{created.Id}", updatePayload);

        // Assert — next GET should return updated data (not stale cached value)
        var getResponse = await _client.GetAsync($"/api/v1/products/{created.Id}");
        var body = await getResponse.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body!.Data.Name.Should().Be("Fresh Name");
        body.Data.Price.Should().Be(50m);
    }

    // --- Helper ---

    private async Task<ProductDto> CreateProductAsync(string name, decimal price)
    {
        var payload = new { Name = name, Price = price };
        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        return body!.Data;
    }
}
