using CleanArchitecture.Application.Common;
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

    // Both authenticated and anonymous requests use the same client — the TestAuthHandler
    // auto-authenticates via the Test scheme, and [AllowAnonymous] endpoints pass through anyway.
    private readonly HttpClient _authenticatedClient = factory.CreateAuthenticatedClient();
    private readonly HttpClient _anonymousClient = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task Update_WhenProductExists_ShouldReturn200WithUpdatedData()
    {
        // Arrange — create a product first
        var created = await CreateProductAsync("Original Name", 10m);

        var updatePayload = new { Name = "Updated Name", Description = "Updated Desc", Price = 99.99m, created.RowVersion };

        // Act
        var response = await _authenticatedClient.PutAsJsonAsync($"/api/v1/products/{created.Id}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body!.Data.Name.Should().Be("Updated Name");
        body.Data.Price.Should().Be(99.99m);
    }

    [Fact]
    public async Task Update_WithNonExistentId_ShouldReturn404()
    {
        var payload = new { Name = "Name", Price = 10m, RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 } };
        var response = await _authenticatedClient.PutAsJsonAsync($"/api/v1/products/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithInvalidData_ShouldReturn422()
    {
        var created = await CreateProductAsync("Original", 10m);
        var payload = new { Name = "", Price = -1m, created.RowVersion };

        var response = await _authenticatedClient.PutAsJsonAsync($"/api/v1/products/{created.Id}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Delete_WhenProductExists_ShouldReturn204()
    {
        // Arrange
        var created = await CreateProductAsync("To Delete", 10m);

        // Act
        var response = await _authenticatedClient.DeleteAsync($"/api/v1/products/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WhenProductExists_ProductShouldBeHiddenAfterDeletion()
    {
        // Arrange
        var created = await CreateProductAsync("To Soft Delete", 10m);

        // Act
        await _authenticatedClient.DeleteAsync($"/api/v1/products/{created.Id}");

        // Assert — GetById should return 404 (soft delete applies query filter)
        var getResponse = await _anonymousClient.GetAsync($"/api/v1/products/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ShouldReturn404()
    {
        var response = await _authenticatedClient.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_AfterCacheHit_ShouldReturnFreshData()
    {
        // Arrange — create and prime cache with first GET
        var created = await CreateProductAsync("Cached Name", 10m);
        await _anonymousClient.GetAsync($"/api/v1/products/{created.Id}"); // prime cache

        // Act — update (invalidates cache)
        var updatePayload = new { Name = "Fresh Name", Price = 50m, created.RowVersion };
        await _authenticatedClient.PutAsJsonAsync($"/api/v1/products/{created.Id}", updatePayload);

        // Assert — next GET should return updated data (not stale cached value)
        var getResponse = await _anonymousClient.GetAsync($"/api/v1/products/{created.Id}");
        var body = await getResponse.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body!.Data.Name.Should().Be("Fresh Name");
        body.Data.Price.Should().Be(50m);
    }

    // --- PATCH Tests ---

    [Fact]
    public async Task Patch_WithNameOnly_ShouldUpdateOnlyName()
    {
        // Arrange — create a product first
        var created = await CreateProductAsync("Original Name", 25.50m);

        var patchPayload = new { created.RowVersion, Name = "Patched Name" };

        // Act
        var response = await _authenticatedClient.PatchAsync($"/api/v1/products/{created.Id}",
            JsonContent.Create(patchPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body!.Data.Name.Should().Be("Patched Name");
        body.Data.Price.Should().Be(25.50m); // unchanged
        body.Data.Description.Should().Be(created.Description); // unchanged
    }

    [Fact]
    public async Task Patch_WithPriceOnly_ShouldUpdateOnlyPrice()
    {
        // Arrange — create a product first
        var created = await CreateProductAsync("Original Name", 25.50m);

        var patchPayload = new { created.RowVersion, Price = 99.99m };

        // Act
        var response = await _authenticatedClient.PatchAsync($"/api/v1/products/{created.Id}",
            JsonContent.Create(patchPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body!.Data.Name.Should().Be("Original Name"); // unchanged
        body.Data.Price.Should().Be(99.99m);
        body.Data.Description.Should().Be(created.Description); // unchanged
    }

    [Fact]
    public async Task Patch_WithDescriptionOnly_ShouldUpdateOnlyDescription()
    {
        // Arrange — create a product first
        var created = await CreateProductAsync("Original Name", 25.50m);

        var patchPayload = new { created.RowVersion, Description = "New description" };

        // Act
        var response = await _authenticatedClient.PatchAsync($"/api/v1/products/{created.Id}",
            JsonContent.Create(patchPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body!.Data.Name.Should().Be("Original Name"); // unchanged
        body.Data.Price.Should().Be(25.50m); // unchanged
        body.Data.Description.Should().Be("New description");
    }

    [Fact]
    public async Task Patch_WithMultipleFields_ShouldUpdateAllSpecifiedFields()
    {
        // Arrange — create a product first
        var created = await CreateProductAsync("Original Name", 25.50m);

        var patchPayload = new {
            created.RowVersion,
            Name = "Patched Name",
            Price = 199.99m,
            Description = "Patched description"
        };

        // Act
        var response = await _authenticatedClient.PatchAsync($"/api/v1/products/{created.Id}",
            JsonContent.Create(patchPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body!.Data.Name.Should().Be("Patched Name");
        body.Data.Price.Should().Be(199.99m);
        body.Data.Description.Should().Be("Patched description");
    }

    [Fact]
    public async Task Patch_WithNonExistentId_ShouldReturn404()
    {
        var patchPayload = new { RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }, Name = "Patched Name" };

        var response = await _authenticatedClient.PatchAsync($"/api/v1/products/{Guid.NewGuid()}",
            JsonContent.Create(patchPayload));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_WithMissingRowVersion_ShouldReturn400()
    {
        // RowVersion is a required non-nullable byte[] on PatchProductCommand.
        // When omitted from the JSON body the model binder rejects it with 400 Bad Request
        // before the FluentValidation pipeline runs.
        var created = await CreateProductAsync("Original Name", 25.50m);
        var patchPayload = new { Name = "Some Name" }; // no RowVersion

        var response = await _authenticatedClient.PatchAsync($"/api/v1/products/{created.Id}",
            JsonContent.Create(patchPayload));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_WithInvalidPrice_ShouldReturn422()
    {
        var created = await CreateProductAsync("Original Name", 25.50m);
        var patchPayload = new { created.RowVersion, Price = -10m };

        var response = await _authenticatedClient.PatchAsync($"/api/v1/products/{created.Id}",
            JsonContent.Create(patchPayload));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Patch_WithEmptyName_ShouldReturn422()
    {
        var created = await CreateProductAsync("Original Name", 25.50m);
        var patchPayload = new { created.RowVersion, Name = "" };

        var response = await _authenticatedClient.PatchAsync($"/api/v1/products/{created.Id}",
            JsonContent.Create(patchPayload));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Patch_AfterCacheHit_ShouldReturnFreshData()
    {
        // Arrange — create and prime cache with first GET
        var created = await CreateProductAsync("Cached Name", 10m);
        await _anonymousClient.GetAsync($"/api/v1/products/{created.Id}"); // prime cache

        // Act — patch (invalidates cache)
        var patchPayload = new { created.RowVersion, Name = "Patched Fresh Name" };
        await _authenticatedClient.PatchAsync($"/api/v1/products/{created.Id}",
            JsonContent.Create(patchPayload));

        // Assert — next GET should return updated data (not stale cached value)
        var getResponse = await _anonymousClient.GetAsync($"/api/v1/products/{created.Id}");
        var body = await getResponse.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body!.Data.Name.Should().Be("Patched Fresh Name");
        body.Data.Price.Should().Be(10m); // unchanged
    }

    [Fact]
    public async Task Delete_ShouldExcludeProductFromPagedList()
    {
        // Arrange — create two products, delete one
        var kept = await CreateProductAsync("Kept Product", 10m);
        var deleted = await CreateProductAsync("Deleted Product", 20m);

        await _authenticatedClient.DeleteAsync($"/api/v1/products/{deleted.Id}");

        // Act — fetch paginated list
        var listResponse = await _anonymousClient.GetAsync("/api/v1/products");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await listResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();

        // Assert — only the non-deleted product appears
        body!.Data.Items.Should().NotContain(p => p.Id == deleted.Id,
            because: "soft-deleted products must be excluded by the global query filter");
        body.Data.Items.Should().Contain(p => p.Id == kept.Id);
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
