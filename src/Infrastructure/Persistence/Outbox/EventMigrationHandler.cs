using System.Text.Json;

namespace CleanArchitecture.Infrastructure.Persistence.Outbox;

/// <summary>
/// Handles migration of domain events when their schema evolves.
///
/// Example: If ProductCreatedEvent adds a new field "SKU" in v2,
/// this handler converts v1 events to v2 before processing.
/// </summary>
public static class EventMigrationHandler
{
    /// <summary>
    /// Attempts to migrate an event from an older version to the current version.
    /// Returns null if migration is not possible.
    /// </summary>
    public static string? MigrateEventJson(string eventJson, string eventType, int fromVersion, int toVersion)
    {
        if (fromVersion == toVersion)
            return eventJson;

        // Example migration paths (add more as your events evolve)
        return eventType switch
        {
            // Example: ProductCreatedEvent v1 -> v2 migration
            // Before: { "productId": "...", "productName": "..." }
            // After:  { "productId": "...", "productName": "...", "productSku": null }
            "CleanArchitecture.Domain.Events.ProductCreatedEvent" when fromVersion == 1 && toVersion == 2
                => MigrateProductCreatedEventV1ToV2(eventJson),

            _ => null  // No migration path found
        };
    }

    private static string? MigrateProductCreatedEventV1ToV2(string eventJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(eventJson);
            var root = doc.RootElement;

            // Create a new JSON object with the additional field
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var v1Event = JsonSerializer.Deserialize<dynamic>(eventJson, jsonOptions);

            // This is a simplified example; in real scenarios, properly handle the JSON structure
            var v2Json = eventJson;  // Your migration logic here
            return v2Json;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
