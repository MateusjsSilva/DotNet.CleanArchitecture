# ADR-013: Testing Strategy — Three-Layer Pyramid

## Status
Accepted

## Context
A Clean Architecture template needs to demonstrate how to test each layer in isolation without coupling tests to infrastructure concerns. We need to cover domain rules, application behavior, architectural constraints, and full HTTP request flows.

## Decision

Three test projects targeting different layers of the pyramid:

```
         ┌─────────────────┐
         │  Integration     │  ~47 tests — real HTTP + real SQL Server
         │  Tests           │  Testcontainers + in-process server
         ├─────────────────┤
         │  Unit Tests      │  ~43 tests — pure logic, no I/O
         │                  │  mocked dependencies (NSubstitute)
         ├─────────────────┤
         │  Architecture    │  ~14 tests — dependency rules
         │  Tests           │  static analysis (NetArchTest)
         └─────────────────┘
```

Total: **101 tests passing**, 3 skipped (intentional — rate-limit tests disabled in CI).

### Unit Tests — `CleanArchitecture.UnitTests`

Dependencies: `Application` + `Domain` projects only. No Infrastructure, no WebAPI.

| Area | What is tested |
|---|---|
| `Domain/` | Entity invariants, domain events, soft delete idempotence |
| `Domain/ProductSoftDeleteTests` | `SoftDelete()` sets `IsDeleted`; `DeletedAt` remains null until `SaveChanges` |
| `Products/` | Command validators (FluentValidation), command handlers (NSubstitute mocks) |
| `Behaviors/` | `CachingBehavior` — cache miss/hit, cache invalidation, passthrough for plain requests |

**Tools**: xUnit + FluentAssertions + NSubstitute + FluentValidation.TestHelper

Handlers are tested by mocking `IProductRepository` and `IUnitOfWork` via NSubstitute, verifying repository method calls and return values without touching the database.

### Architecture Tests — `CleanArchitecture.ArchitectureTests`

Enforces Clean Architecture dependency rules using NetArchTest:

- Domain must not reference Application, Infrastructure, or WebAPI.
- Application must not reference Infrastructure or WebAPI.
- Infrastructure must not reference WebAPI.
- Controllers must not reference Domain or Infrastructure directly (only Application DTOs/Commands/Queries).
- Use case handlers must be `internal` — not part of the public API surface.
- Validators must reside in the Application layer.
- Commands must not also implement `IQuery` (CQRS purity).
- Handlers must not depend on other handlers (no handler-to-handler coupling).

Failures here indicate a layer boundary violation that slipped through code review.

### Integration Tests — `CleanArchitecture.IntegrationTests`

Spins up the full ASP.NET Core pipeline in-process via `WebApplicationFactory<Program>`.
Uses **Testcontainers** (`Testcontainers.MsSqlServer`) with a real SQL Server container, reset between test classes via `Respawn`.

| Test class | Scenarios covered |
|---|---|
| `ProductsQueryTests` | GET all (pagination, filtering, ordering), GET by ID, GET summary |
| `ProductsMutationTests` | POST, PUT valid/404/422/409, PATCH valid/404/422/400, DELETE 204/404, soft delete hides record, **cache invalidation E2E** |
| `RateLimitingTests` | 429 enforcement, Problem Details format (skipped in CI — requires real rate limiter, disabled in Test environment) |

**Shared factory via `ICollectionFixture`**: all test classes share a single `WebApplicationFactory` + `MsSqlContainer` instance (defined in `IntegrationTestCollection`). This prevents Serilog's `ReloadableLogger` from being frozen twice when xUnit runs parallel test classes.

**Authentication**: integration tests use a `TestAuthHandler` that auto-authenticates all requests under the `Test` scheme. `CreateAuthenticatedClient()` returns a pre-configured `HttpClient`; `[AllowAnonymous]` endpoints are also tested.

### Why Testcontainers instead of EF InMemory

| | EF InMemory | Testcontainers (SQL Server) |
|---|---|---|
| Speed | ⚡ ~4 s suite | 🐢 ~30 s (Docker pull + startup) |
| Docker required | ❌ No | ✅ Yes |
| SQL Server-specific behavior | ❌ Not tested | ✅ Tested |
| Unique constraints | ❌ Not enforced | ✅ Enforced |
| Optimistic concurrency (RowVersion) | ❌ Not enforced | ✅ Enforced |
| Migrations validated | ❌ No | ✅ Yes |

Testcontainers is used because this template includes SQL Server-specific features (`rowversion`, unique indexes, global query filters) that EF InMemory does not validate. The extra startup time is acceptable for a template that prioritizes correctness over CI speed.

## Consequences
- **Positive**: Tests run against real SQL Server — unique constraints, RowVersion concurrency, and EF migrations are all exercised.
- **Positive**: Each layer is tested in isolation — unit tests don't start the HTTP stack; architecture tests don't execute any code.
- **Positive**: The cache invalidation test (`Update_AfterCacheHit_ShouldReturnFreshData`, `Patch_AfterCacheHit_ShouldReturnFreshData`) demonstrates the full read→mutate→read cycle with real pipeline behaviors running.
- **Positive**: Rate limiting tests are in the suite but skipped in Test environments — they document the expected behavior without blocking CI.
- **Negative**: Integration tests require Docker. CI pipelines without Docker support must use a different runner or skip integration tests.
- **Negative**: No load or contract tests. For APIs with external consumers, add Pact (consumer-driven contracts) or k6 (load testing).
