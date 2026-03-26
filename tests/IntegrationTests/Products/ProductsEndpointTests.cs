using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.WebAPI.Models;
using System.Net;
using System.Net.Http.Json;

namespace CleanArchitecture.IntegrationTests.Products;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProductsEndpointTests(WebApplicationFactoryFixture factory) : IAsyncLifetime
{
    private readonly HttpClient _anonymousClient = factory.CreateAnonymousClient();
    private readonly HttpClient _authenticatedClient = factory.CreateAuthenticatedClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_ShouldReturn200WithPagedResult()
    {
        // Act - GET endpoints are public (AllowAnonymous)
        var response = await _anonymousClient.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
        body.Should().NotBeNull();
        body!.Data.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithValidCommand_ShouldReturn201Created()
    {
        // Arrange
        var command = new { Name = "Integration Test Product", Description = "Test", Price = 49.99m };

        // Act - POST requires authentication
        var response = await _authenticatedClient.PostAsJsonAsync("/api/v1/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body.Should().NotBeNull();
        body!.Data.Name.Should().Be("Integration Test Product");
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var command = new { Name = "Test Product", Description = "Test", Price = 49.99m };

        // Act - POST without authentication
        var response = await _anonymousClient.PostAsJsonAsync("/api/v1/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Act - GET endpoints are public
        var response = await _anonymousClient.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithInvalidData_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var command = new { Name = "", Price = -1m };

        // Act - POST requires authentication
        var response = await _authenticatedClient.PostAsJsonAsync("/api/v1/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
