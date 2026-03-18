# ADR-015: Production-Ready Enhancements

**Date**: 2026-03-18
**Status**: Accepted
**Context**: Making the template suitable for production deployment with reliability, resilience, and operational maturity.

---

## Problem

The Outbox Pattern alone doesn't guarantee production readiness. We need:

1. **Reliability**: Retry failed events with exponential backoff
2. **Dead Letter Queue**: Route permanently failed messages for investigation
3. **Event Versioning**: Support schema evolution without data loss
4. **Idempotency**: Prevent duplicate processing of retried events
5. **Seeding**: Pre-populate development database with realistic data
6. **Caching**: Reduce database load with intelligent expiration

## Decisions

### 1. Polly Resilience Library for Retry Policy

**Decision**: Add exponential backoff retry policy to `OutboxProcessorService`.

**Implementation** (`src/Infrastructure/Persistence/Outbox/OutboxProcessorService.cs`):

```csharp
private IAsyncPolicy RetryPolicy =>
    _retryPolicy ??= Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            retryCount: MaxRetries,  // 3 retries
            sleepDurationProvider: attempt =>
                TimeSpan.FromSeconds(Math.Pow(2, attempt)));  // 2s, 4s, 8s
```

**Advantages**:
- ✅ Industry-standard resilience (Microsoft uses it internally)
- ✅ Exponential backoff prevents overwhelming failing services
- ✅ Reduces transient failure impact
- ✅ Observable via logs

**Added**: `Polly` v8.4.1 NuGet package

---

### 2. Dead Letter Queue (DLQ) for Failed Messages

**Decision**: Move messages to `DeadLetterMessages` table after max retries.

**Schema** (`src/Infrastructure/Persistence/Outbox/DeadLetterMessage.cs`):

```csharp
public class DeadLetterMessage
{
    public Guid Id { get; init; }
    public Guid OutboxMessageId { get; set; }
    public string Type { get; set; }
    public string Content { get; set; }
    public int EventVersion { get; set; }
    public string Error { get; set; }
    public int RetryCount { get; set; }
    public DateTime FailedAt { get; set; }
    public DateTime? ReprocessedAt { get; set; }
}
```

**Advantages**:
- ✅ Prevents data loss of failed events
- ✅ Enables investigation and manual reprocessing
- ✅ Audit trail of failures
- ✅ Monitoring: query for recent failures

**Usage**:
```sql
-- Find failed messages
SELECT * FROM DeadLetterMessages
WHERE FailedAt > DATEADD(hour, -1, GETUTCDATE())
ORDER BY FailedAt DESC;

-- Manual reprocess (business logic to retry)
SELECT Content FROM DeadLetterMessages WHERE Id = @id;
```

---

### 3. Event Versioning & Migration Handler

**Decision**: Support schema evolution via version field and migration framework.

**Schema Addition** (`OutboxMessage`):

```csharp
public int EventVersion { get; init; } = 1;
```

**Migration Handler** (`src/Infrastructure/Persistence/Outbox/EventMigrationHandler.cs`):

```csharp
public static string? MigrateEventJson(
    string eventJson,
    string eventType,
    int fromVersion,
    int toVersion)
{
    if (fromVersion == toVersion) return eventJson;

    return eventType switch
    {
        // Example: ProductCreatedEvent v1 -> v2
        // Before: { "productId": "...", "productName": "..." }
        // After:  { "productId": "...", "productName": "...", "productSku": null }
        "CleanArchitecture.Domain.Events.ProductCreatedEvent"
            when fromVersion == 1 && toVersion == 2
            => MigrateProductCreatedEventV1ToV2(eventJson),

        _ => null  // No migration path found
    };
}
```

**Migration Example**:
```csharp
private static string? MigrateProductCreatedEventV1ToV2(string eventJson)
{
    try
    {
        using var doc = JsonDocument.Parse(eventJson);
        var root = doc.RootElement;

        // Add "productSku" field with default value
        var v2 = new JsonObject
        {
            ["productId"] = root.GetProperty("productId").GetString(),
            ["productName"] = root.GetProperty("productName").GetString(),
            ["productSku"] = "UNKNOWN"  // New field
        };

        return JsonConvert.SerializeObject(v2);
    }
    catch
    {
        return null;
    }
}
```

**Advantages**:
- ✅ Safe schema evolution
- ✅ No data loss during deployments
- ✅ Backward compatibility
- ✅ Graceful degradation

---

### 4. Idempotency via IdempotencyKey

**Decision**: Add `IdempotencyKey` field to prevent duplicate processing.

**Schema Addition** (`OutboxMessage`):

```csharp
public string? IdempotencyKey { get; init; }
```

**Implementation**:

```csharp
// In OutboxProcessorService
private async Task ProcessMessageAsync(OutboxMessage message)
{
    // Check if already processed
    var alreadyProcessed = await CheckIdempotencyAsync(message.IdempotencyKey);
    if (alreadyProcessed)
    {
        _logger.LogInformation(
            "Idempotent event already processed: {IdempotencyKey}",
            message.IdempotencyKey);
        return;
    }

    try
    {
        var eventType = Type.GetType(message.Type)!;
        var domainEvent = JsonSerializer.Deserialize(message.Content, eventType)!;

        // Process event
        var handler = GetHandler(eventType);
        await handler.Handle(domainEvent);

        // Mark as processed
        await MarkIdempotencyKeyAsync(message.IdempotencyKey);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process event: {Type}", message.Type);
        throw;
    }
}
```

**Advantages**:
- ✅ Prevents duplicate side effects
- ✅ Safe for message retries
- ✅ Essential for exactly-once semantics
- ✅ Supports idempotent consumers

---

### 5. Development Data Seeding

**Decision**: Auto-seed development database with realistic sample data.

**Implementation** (`src/Infrastructure/Persistence/ApplicationDbContextSeeder.cs`):

```csharp
public static async Task SeedAsync(ApplicationDbContext db)
{
    if (await db.Products.AnyAsync())
        return;  // Already seeded

    // Add sample products
    var products = new List<Product>
    {
        Product.Create(
            name: "Laptop Pro",
            description: "High-performance laptop",
            price: 1299.99m
        ),
        Product.Create(
            name: "Wireless Mouse",
            description: "Ergonomic wireless mouse",
            price: 49.99m
        ),
        // ... more products
    };

    await db.Products.AddRangeAsync(products);
    await db.SaveChangesAsync();
}
```

**Activation** (`Program.cs`):

```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await ApplicationDbContextSeeder.SeedAsync(db);
}
```

**Advantages**:
- ✅ No manual database population
- ✅ Consistent data for all developers
- ✅ Faster onboarding
- ✅ Realistic testing data

---

### 6. Cache with Sliding Window Expiration

**Decision**: Extend `ICacheableQuery` to support sliding expiration.

**Interface Update** (`src/Application/Common/ICacheableQuery.cs`):

```csharp
public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? AbsoluteExpiration => null;
    TimeSpan? SlidingExpiration => null;
}
```

**Strategy**:
- **Absolute Expiration**: For static data (e.g., product details, 10 min)
- **Sliding Expiration**: For dynamic data (e.g., product listings, 5 min after last access)

**Implementation** (`src/Application/Behaviors/CachingBehavior.cs`):

```csharp
private static DistributedCacheEntryOptions BuildCacheOptions(ICacheableQuery cacheableRequest)
{
    var options = new DistributedCacheEntryOptions();

    if (cacheableRequest.AbsoluteExpiration.HasValue)
    {
        options.AbsoluteExpirationRelativeToNow = cacheableRequest.AbsoluteExpiration.Value;
    }
    else if (cacheableRequest.SlidingExpiration.HasValue)
    {
        options.SlidingExpiration = cacheableRequest.SlidingExpiration.Value;
    }
    else
    {
        options.AbsoluteExpirationRelativeToNow = DefaultExpiration;
    }

    return options;
}
```

**Examples**:
```csharp
// Product listings (changes frequently, benefits from refresh-on-access)
public sealed record GetAllProductsQuery : ICacheableQuery
{
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
}

// Product details (stable data)
public sealed record GetProductByIdQuery : ICacheableQuery
{
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(10);
}
```

**Advantages**:
- ✅ Reduced database load
- ✅ Intelligent expiration (access-aware)
- ✅ Configurable per-query
- ✅ Transparent caching in MediatR pipeline

---

## Testing

### Integration Tests Added

**`tests/IntegrationTests/RateLimiting/RateLimitingTests.cs`**:
- Rate limit enforcement (429 responses)
- Problem Details format validation
- Concurrent request handling

**Existing Outbox Tests** (via seeded data):
- Domain events processing
- Idempotency verification
- Event versioning scenarios

### Manual Testing Checklist

```bash
# 1. Verify seeding
dotnet run --environment Development
# Check: Products visible in database/API

# 2. Test cache with sliding expiration
curl http://localhost:5000/api/v1/products
# Wait 5 min of no access, cache should expire

# 3. Test rate limiting
for i in {1..51}; do
  curl http://localhost:5000/api/v1/products
done
# Request 51 should return 429 Too Many Requests

# 4. Test event retry + DLQ (inject failure)
# Temporarily make OutboxProcessorService throw
# Verify: message in DeadLetterMessages after 3 retries

# 5. Test event versioning (schema evolution)
# Publish v1 event, upgrade to v2
# Verify: old events migrated automatically
```

---

## Deployment Checklist

- [ ] EF Core migrations applied (includes DLQ table)
- [ ] Outbox Processor configured to run
- [ ] Redis cache configured (or InMemory for dev)
- [ ] Polly retry policies tested
- [ ] DLQ monitoring alerts configured
- [ ] Event migration handlers reviewed
- [ ] Rate limiting limits tuned per environment
- [ ] Integration tests passing

---

## Monitoring & Observability

### Key Metrics

1. **Outbox Processor**:
   - Messages processed/sec
   - Retry rate
   - DLQ entries

2. **Rate Limiting**:
   - 429 responses/sec
   - Top limiting endpoints

3. **Cache**:
   - Hit rate (%)
   - Evictions/sec

### Logging

```csharp
// Outbox Processing
_logger.LogInformation("Processing {Count} outbox messages", messages.Count);
_logger.LogWarning("Event {Type} failed, moving to DLQ", message.Type);
_logger.LogInformation("Idempotent event already processed: {Key}", message.IdempotencyKey);

// Cache
_logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
_logger.LogDebug("Cache miss for {CacheKey}, querying database", cacheKey);
```

---

## Migration Path

### From Previous Template (v1)

If upgrading from template without these enhancements:

```sql
-- Add new columns
ALTER TABLE OutboxMessages
ADD EventVersion INT DEFAULT 1,
    RetryCount INT DEFAULT 0,
    IdempotencyKey NVARCHAR(MAX);

-- Create DLQ table (via EF migration)
dotnet ef migrations add AddDeadLetterQueue
dotnet ef database update

-- Run seeding for dev
// In Program.cs, seed only runs in Development
```

---

## Roadmap

### Current (ADR-015)
- ✅ Polly resilience with exponential backoff
- ✅ Dead Letter Queue for failed events
- ✅ Event versioning framework
- ✅ Idempotency key support
- ✅ Development seeding
- ✅ Sliding window caching

### Future (Phase 2)
- Redis for distributed cache
- Per-tenant rate limiting
- Event dead letter reprocessing UI
- Outbox metrics dashboard

### Future (Phase 3)
- NATS/RabbitMQ integration
- Event sourcing layer
- CQRS read model projections

---

## References

- [Polly Resilience Library](https://github.com/App-vNext/Polly)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Event Versioning](https://en.wikipedia.org/wiki/Schema_evolution)
- [Idempotency Keys](https://stripe.com/blog/idempotency)
- [Exponential Backoff](https://en.wikipedia.org/wiki/Exponential_backoff)

## Related ADRs

- [ADR-005: Domain Events & Outbox Pattern](ADR-005-domain-events-outbox.md)
- [ADR-009: Caching Strategy](ADR-009-caching.md)
- [ADR-014: Rate Limiting Strategy](ADR-014-rate-limiting-strategy.md)
