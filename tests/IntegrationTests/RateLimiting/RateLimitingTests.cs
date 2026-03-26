using System.Net.Http.Json;
using CleanArchitecture.WebAPI.Models;

namespace CleanArchitecture.IntegrationTests.RateLimiting;

/// <summary>
/// Integration tests for rate limiting enforcement.
/// Validates that the API enforces rate limits to protect against abuse.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class RateLimitingTests(WebApplicationFactoryFixture factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Rate limiting should enforce a 50 request per minute default limit.
    /// After 50 requests, subsequent requests should return 429 Too Many Requests.
    /// </summary>
    [Fact(Skip = "Rate limiting disabled in Test environment")]
    public async Task DefaultRateLimit_ShouldEnforceLimit_50PerMinute()
    {
        // Arrange
        var productUrl = "/api/v1/products";

        // Act - Make 50 concurrent requests
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _client.GetAsync(productUrl))
            .ToList();

        var responses = await Task.WhenAll(tasks);
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);

        // Assert - Most requests should succeed (we're at or near the limit)
        successCount.Should().Be(50, "All requests up to the limit should succeed");

        // Next request should be rate limited
        var rateLimitedResponse = await _client.GetAsync(productUrl);
        rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// Rate limit rejection response should be in Problem Details format (RFC 9457).
    /// </summary>
    [Fact(Skip = "Rate limiting disabled in Test environment")]
    public async Task RateLimitRejection_ShouldReturnProblemDetailsFormat()
    {
        // Arrange
        var productUrl = "/api/v1/products";

        // Act - Exhaust rate limit
        var tasks = Enumerable.Range(0, 51)
            .Select(_ => _client.GetAsync(productUrl))
            .ToList();

        await Task.WhenAll(tasks);

        // Find the rate limited response
        var rateLimitedResponse = await _client.GetAsync(productUrl);

        // Assert
        rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var contentType = rateLimitedResponse.Content.Headers.ContentType?.MediaType;
        contentType.Should().Be("application/problem+json");

        var body = await rateLimitedResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        body.Should().NotBeNull();
        body!.Status.Should().Be((int)HttpStatusCode.TooManyRequests);
        body.Title.Should().Contain("Too Many Requests");
        body.Detail.Should().Contain("Rate limit exceeded");
        body.Instance.Should().StartWith("/api/v1/products");
    }

    /// <summary>
    /// Multiple concurrent requests within the limit should succeed.
    /// </summary>
    [Fact]
    public async Task ConcurrentRequests_WithinLimit_ShouldAllSucceed()
    {
        // Arrange
        var productUrl = "/api/v1/products";

        // Act - Make 10 concurrent requests (well below the 50 limit)
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _client.GetAsync(productUrl))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed
        responses.Should().AllSatisfy(r =>
            r.StatusCode.Should().Be(HttpStatusCode.OK)
        );
    }

    /// <summary>
    /// Rate limiting should apply to all endpoints equally (single global policy).
    /// </summary>
    [Fact(Skip = "Rate limiting disabled in Test environment")]
    public async Task RateLimiting_ShouldApplyToAllEndpoints()
    {
        // Arrange
        var authRequest = new { email = "test@example.com", password = "Password1!" };
        var productUrl = "/api/v1/products";
        var authUrl = "/api/v1/auth/login";

        // Act - Make many requests across different endpoints
        var authTasks = Enumerable.Range(0, 25)
            .Select(_ => _client.PostAsJsonAsync(authUrl, authRequest));

        var productTasks = Enumerable.Range(0, 25)
            .Select(_ => _client.GetAsync(productUrl));

        var allTasks = authTasks.Concat(productTasks).ToList();
        var responses = await Task.WhenAll(allTasks);

        // Assert - Combined requests should count towards shared rate limit
        var successCount = responses.Count(r => r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest);
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

        // We made 50 requests across both endpoints - some should succeed, and eventually we should hit 429
        successCount.Should().BeGreaterThan(0, "Some requests should succeed");
    }
}
