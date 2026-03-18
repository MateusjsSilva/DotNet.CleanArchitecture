# ADR-014: Rate Limiting Strategy

**Date**: 2026-03-18
**Status**: Accepted
**Context**: Protecting API from abuse while maintaining usability for legitimate clients.

---

## Problem

A public API needs protection against:
- Brute force attacks (auth endpoints)
- DoS attacks (resource exhaustion)
- Unfair resource distribution (quota fairness)

Different endpoint types require different limits:
- Auth endpoints: very strict (5 req/min) to prevent credential stuffing
- Standard CRUD: moderate (100 req/min) for normal operations
- Resource-intensive (AI): relaxed (30 req/min) due to computation cost
- Others: sensible default (50 req/min)

## Decision

Implement **granular rate limiting** using ASP.NET Core's built-in `RateLimiter` middleware with multiple fixed-window policies.

### Configuration

Four policies are defined in `RateLimitingExtensions.cs`:

| Policy | Limit | Window | Queue | Purpose |
|--------|-------|--------|-------|---------|
| `auth` | 5 req/min | 1 minute | 2 | Brute force prevention |
| `products` | 100 req/min | 1 minute | 10 | Standard CRUD operations |
| `ai` | 30 req/min | 1 minute | 5 | Resource-intensive operations |
| `default` | 50 req/min | 1 minute | 5 | Fallback for other endpoints |

### Implementation

```csharp
// src/WebAPI/Extensions/RateLimitingExtensions.cs
public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
{
    services.AddRateLimiter(options =>
    {
        // Define each policy with FixedWindowLimiter
        options.AddFixedWindowLimiter("auth", cfg => { ... });
        options.AddFixedWindowLimiter("products", cfg => { ... });

        // OnRejected returns Problem Details (RFC 9457)
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.ContentType = "application/problem+json";
            await context.HttpContext.Response.WriteAsJsonAsync(
                new ProblemDetails {
                    Status = 429,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please try again later."
                },
                // ...
            );
        };
    });
}
```

### Application

In `Program.cs`:

```csharp
app.UseRateLimiter();  // BEFORE authentication
app.MapControllers().RequireRateLimiting(RateLimitingExtensions.DefaultPolicy);
```

All endpoints get the default policy. Future: customize per-controller via convention.

## Responses

When rate limited (429 Too Many Requests):

```json
HTTP/1.1 429 Too Many Requests
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.30",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Please try again later.",
  "instance": "/api/v1/auth/login"
}
```

## Advantages

✅ **Built-in**: No external dependencies (ASP.NET Core 10 includes `System.Threading.RateLimiting`)
✅ **Performance**: Fixed window is simple and efficient
✅ **Observability**: Integrates with logs and can be extended to metrics
✅ **Standard Format**: Problem Details (RFC 9457) for all errors
✅ **Queue Support**: Fairness via queue with OldestFirst processing

## Disadvantages

❌ **In-Process Only**: Not suitable for load-balanced farms (no distributed state)
❌ **Per-Connection**: Limited to IP-based identification (no per-user tracking)
❌ **Fixed Window**: Can burst at window boundaries (use sliding window for strictness)
❌ **Global Counter**: All endpoints share the same limit initially (no per-endpoint tracking)

## Future Improvements

### Phase 2: Distributed Rate Limiting
- Add Redis backend for distributed counters
- Enable farm/load-balance scenarios
- Centralized rate limit state

### Phase 3: Advanced Policies
- Per-user rate limiting (authenticated endpoints)
- Per-tenant rate limiting (multi-tenant systems)
- Time-based escalation (stricter limits during high load)

### Phase 4: Observability
- Export rate limit metrics to Prometheus
- Grafana dashboard for violations
- Alerts for abuse patterns

## Testing

Integration tests in `tests/IntegrationTests/RateLimiting/RateLimitingTests.cs`:

```csharp
[Fact]
public async Task DefaultRateLimit_ShouldEnforceLimit_50PerMinute()
{
    // Make 50 requests - all succeed
    var tasks = Enumerable.Range(0, 50)
        .Select(_ => _client.GetAsync("/api/v1/products"))
        .ToList();

    var responses = await Task.WhenAll(tasks);
    responses.Should().AllSatisfy(r => r.StatusCode == 200);

    // 51st request is rate limited
    var limited = await _client.GetAsync("/api/v1/products");
    limited.StatusCode.Should().Be(429);
}
```

## References

- [ASP.NET Core Rate Limiting Middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limiting)
- [RFC 9110 - HTTP Semantics (429 status)](https://tools.ietf.org/html/rfc9110#section-15.5.30)
- [RFC 9457 - Problem Details](https://tools.ietf.org/html/rfc9457)
- [System.Threading.RateLimiting API](https://learn.microsoft.com/en-us/dotnet/api/system.threading.ratelimiting)
- [Rate Limiting Algorithms](https://en.wikipedia.org/wiki/Rate_limiting)

## Related ADRs

- [ADR-010: Problem Details](ADR-010-problem-details.md) — Error format standard
- [ADR-008: Observability](ADR-008-observability.md) — Metrics and monitoring
