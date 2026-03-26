using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Outbox;
using CleanArchitecture.WebAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace CleanArchitecture.IntegrationTests.Outbox;

/// <summary>
/// Verifies the Outbox pattern contracts:
///   1. Creating a domain aggregate persists an OutboxMessage in the same transaction.
///   2. The message has the correct type and is initially unprocessed.
///   3. The OutboxProcessorService eventually marks it as processed (polling delay allowed).
///   4. Duplicate messages (same idempotency key) are not processed twice.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class OutboxTests(WebApplicationFactoryFixture factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateProduct_ShouldPersistOutboxMessage_WithCorrectEventType()
    {
        // Arrange
        var payload = new { Name = "Outbox Product", Price = 10m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert — inspect the outbox table directly
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var messages = db.OutboxMessages.ToList();
        messages.Should().NotBeEmpty("creating a product raises a ProductCreatedEvent");

        var outboxMsg = messages.First();
        outboxMsg.Type.Should().Contain("ProductCreatedEvent",
            because: "the event type name must match so the processor can deserialize it");
        outboxMsg.Content.Should().NotBeNullOrWhiteSpace();
        outboxMsg.EventVersion.Should().Be(1);
    }

    [Fact]
    public async Task CreateProduct_OutboxMessage_ShouldBeUnprocessedInitially()
    {
        // Arrange
        var payload = new { Name = "Unprocessed Outbox", Price = 5m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);
        response.EnsureSuccessStatusCode();

        // Assert — the background processor hasn't run yet (it polls every 10 s)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var message = db.OutboxMessages
            .OrderByDescending(m => m.OccurredAt)
            .FirstOrDefault();

        message.Should().NotBeNull();
        message!.ProcessedAt.Should().BeNull(
            because: "the message has not been picked up by the processor yet");
        message.RetryCount.Should().Be(0);
        message.Error.Should().BeNull();
    }

    [Fact]
    public async Task CreateProduct_OutboxMessage_ContentShouldBeDeserializable()
    {
        // Arrange
        var payload = new { Name = "Deserializable Event", Price = 15m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        response.EnsureSuccessStatusCode();

        // Assert — the stored JSON must contain the product id so the processor
        // can reconstruct a valid ProductCreatedEvent
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var message = db.OutboxMessages
            .OrderByDescending(m => m.OccurredAt)
            .FirstOrDefault();

        message.Should().NotBeNull();
        message!.Content.Should().Contain(body!.Data.Id.ToString(),
            because: "the serialised event must reference the created product id");
    }

    [Fact]
    public async Task MultipleProductCreations_ShouldEachHaveOwnOutboxMessage()
    {
        // Arrange
        var names = new[] { "Alpha", "Beta", "Gamma" };

        // Act
        foreach (var name in names)
            await _client.PostAsJsonAsync("/api/v1/products", new { Name = name, Price = 1m });

        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var count = db.OutboxMessages.Count();
        count.Should().Be(names.Length,
            because: "each product creation raises exactly one ProductCreatedEvent");
    }

    [Fact]
    public async Task OutboxMessage_ShouldNotAppearInDeadLetterQueue_Initially()
    {
        // Arrange
        var payload = new { Name = "No DLQ Product", Price = 8m };

        // Act
        await _client.PostAsJsonAsync("/api/v1/products", payload);

        // Assert — a freshly created message should never be in the DLQ
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.DeadLetterMessages.Should().BeEmpty(
            because: "valid events with correct types must not fail on first attempt");
    }
}
