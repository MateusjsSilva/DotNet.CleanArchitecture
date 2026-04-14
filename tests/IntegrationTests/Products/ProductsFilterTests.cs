using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.WebAPI.Models;
using System.Net;
using System.Net.Http.Json;

namespace CleanArchitecture.IntegrationTests.Products;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProductsFilterTests(WebApplicationFactoryFixture factory) : IAsyncLifetime
{
    private readonly HttpClient _authenticatedClient = factory.CreateAuthenticatedClient();
    private readonly HttpClient _anonymousClient = factory.CreateAuthenticatedClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_WithNameContains_ShouldReturnOnlyMatchingProducts()
    {
        // Arrange
        await CreateProductAsync("Alpha Widget", 10m);
        await CreateProductAsync("Beta Gadget", 20m);
        await CreateProductAsync("Alpha Gadget", 30m);

        // Act
        var response = await _anonymousClient.GetAsync("/api/v1/products?nameContains=alpha");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
        body!.Data.Items.Should().HaveCount(2);
        body.Data.Items.Should().AllSatisfy(p =>
            p.Name.ToLower().Should().Contain("alpha"));
    }

    [Fact]
    public async Task GetAll_WithMinPrice_ShouldReturnOnlyProductsAboveThreshold()
    {
        // Arrange
        await CreateProductAsync("Cheap", 5m);
        await CreateProductAsync("Mid", 50m);
        await CreateProductAsync("Expensive", 200m);

        // Act
        var response = await _anonymousClient.GetAsync("/api/v1/products?minPrice=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
        body!.Data.Items.Should().HaveCount(2);
        body.Data.Items.Should().AllSatisfy(p => p.Price.Should().BeGreaterThanOrEqualTo(50m));
    }

    [Fact]
    public async Task GetAll_WithMaxPrice_ShouldReturnOnlyProductsBelowThreshold()
    {
        // Arrange
        await CreateProductAsync("Cheap", 5m);
        await CreateProductAsync("Mid", 50m);
        await CreateProductAsync("Expensive", 200m);

        // Act
        var response = await _anonymousClient.GetAsync("/api/v1/products?maxPrice=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
        body!.Data.Items.Should().HaveCount(2);
        body.Data.Items.Should().AllSatisfy(p => p.Price.Should().BeLessThanOrEqualTo(50m));
    }

    [Fact]
    public async Task GetAll_WithPriceRange_ShouldReturnOnlyProductsInRange()
    {
        // Arrange
        await CreateProductAsync("Very Cheap", 5m);
        await CreateProductAsync("Mid", 50m);
        await CreateProductAsync("Premium", 150m);
        await CreateProductAsync("Luxury", 500m);

        // Act
        var response = await _anonymousClient.GetAsync("/api/v1/products?minPrice=40&maxPrice=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
        body!.Data.Items.Should().HaveCount(2);
        body.Data.Items.Select(p => p.Name).Should().BeEquivalentTo(["Mid", "Premium"]);
    }

    [Fact]
    public async Task GetAll_WithNameAndPriceFilter_ShouldCombineFilters()
    {
        // Arrange
        await CreateProductAsync("Widget A", 10m);
        await CreateProductAsync("Widget B", 100m);
        await CreateProductAsync("Gadget A", 10m);

        // Act
        var response = await _anonymousClient.GetAsync("/api/v1/products?nameContains=widget&minPrice=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
        body!.Data.Items.Should().HaveCount(1);
        body.Data.Items.Single().Name.Should().Be("Widget B");
    }

    [Fact]
    public async Task GetAll_WithNoMatchingFilter_ShouldReturnEmptyItems()
    {
        // Arrange
        await CreateProductAsync("Product", 10m);

        // Act
        var response = await _anonymousClient.GetAsync("/api/v1/products?nameContains=nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
        body!.Data.Items.Should().BeEmpty();
        body.Data.TotalCount.Should().Be(0);
    }

    // --- Helper ---

    private async Task<ProductDto> CreateProductAsync(string name, decimal price)
    {
        var payload = new { Name = name, Price = price };
        var response = await _authenticatedClient.PostAsJsonAsync("/api/v1/products", payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        return body!.Data;
    }
}
