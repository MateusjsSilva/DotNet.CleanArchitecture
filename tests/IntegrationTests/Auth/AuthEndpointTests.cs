using CleanArchitecture.Application.DTOs;
using CleanArchitecture.WebAPI.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CleanArchitecture.IntegrationTests.Auth;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthEndpointTests(WebApplicationFactoryFixture factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private readonly HttpClient _client = factory.CreateClient();

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidCredentials_ShouldReturn201WithTokens()
    {
        var payload = new { Email = "user@test.com", Password = "Test@1234!", FullName = "Test User" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokensDto>>();
        body!.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.Data.AccessTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn400()
    {
        var payload = new { Email = "dup@test.com", Password = "Test@1234!" };

        await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200WithTokens()
    {
        const string email = "login@test.com";
        const string password = "Test@1234!";

        await RegisterAsync(email, password);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokensDto>>();
        body!.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        await RegisterAsync("wrong@test.com", "Test@1234!");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Email = "wrong@test.com", Password = "WrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Email = "nobody@test.com", Password = "Test@1234!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithValidToken_ShouldReturn200WithNewTokens()
    {
        var tokens = await RegisterAsync("refresh@test.com", "Test@1234!");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { RefreshToken = tokens.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokensDto>>();
        body!.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        // The new refresh token must differ from the old one (token rotation)
        body.Data.RefreshToken.Should().NotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ShouldReturn401()
    {
        var tokens = await RegisterAsync("refresh2@test.com", "Test@1234!");

        // Revoke first
        await _client.PostAsJsonAsync("/api/v1/auth/revoke", new { RefreshToken = tokens.RefreshToken });

        // Then try to refresh with the revoked token
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { RefreshToken = tokens.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { RefreshToken = "this-is-not-a-valid-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Revoke ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Revoke_WithValidToken_ShouldReturn204()
    {
        var tokens = await RegisterAsync("revoke@test.com", "Test@1234!");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/revoke",
            new { RefreshToken = tokens.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoke_TwiceWithSameToken_ShouldReturn401OnSecondCall()
    {
        var tokens = await RegisterAsync("revoke2@test.com", "Test@1234!");

        await _client.PostAsJsonAsync("/api/v1/auth/revoke", new { RefreshToken = tokens.RefreshToken });

        var second = await _client.PostAsJsonAsync(
            "/api/v1/auth/revoke",
            new { RefreshToken = tokens.RefreshToken });

        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Protected endpoint ────────────────────────────────────────────────────

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ShouldNotReturn401()
    {
        var tokens = await RegisterAsync("protected@test.com", "Test@1234!");

        using var authClient = factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // We just verify the request is not rejected with 401 (AI may return 400/500 without config)
        var response = await authClient.PostAsJsonAsync(
            "/api/v1/ai/complete",
            new { Prompt = "Hello" });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<AuthTokensDto> RegisterAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { Email = email, Password = password });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokensDto>>();
        return body!.Data;
    }
}
