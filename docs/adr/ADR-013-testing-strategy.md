# ADR-013: Testing Strategy — Three-Layer Pyramid

## Status
Accepted

## Context
A Clean Architecture template needs to demonstrate how to test each layer in isolation without coupling tests to infrastructure concerns. We need to cover domain rules, application behavior, architectural constraints, and full HTTP request flows.

## Decision

Three test projects targeting different layers of the pyramid:

```
         ┌─────────────────┐
         │  Integration     │  11 tests — real HTTP stack
         │  Tests           │  in-memory DB + in-process server
         ├─────────────────┤
         │  Unit Tests      │  33 tests — pure logic, no I/O
         │                  │  mocked dependencies (NSubstitute)
         ├─────────────────┤
         │  Architecture    │   7 tests — dependency rules
         │  Tests           │  static analysis (NetArchTest)
         └─────────────────┘
```

### Unit Tests — `CleanArchitecture.UnitTests`

Dependencies: `Application` + `Domain` projects only. No Infrastructure, no WebAPI.

| Area | What is tested |
|---|---|
| `Domain/` | Entity invariants, domain events, soft delete idempotence |
| `Products/` | Command validators (FluentValidation), command handlers (NSubstitute mocks) |
| `Behaviors/` | `CachingBehavior` — cache miss/hit, cache invalidation, passthrough for plain requests |

**Tools**: xUnit + FluentAssertions + NSubstitute + FluentValidation.TestHelper

Handlers are tested by mocking `IProductRepository` and `IUnitOfWork` via NSubstitute, verifying repository method calls and return values without touching the database.

### Architecture Tests — `CleanArchitecture.ArchitectureTests`

Enforces Clean Architecture dependency rules using NetArchTest:

- Domain must not reference Application, Infrastructure, or WebAPI.
- Application must not reference Infrastructure or WebAPI.
- Infrastructure must not reference WebAPI.
- Controllers must not reference Domain directly (only Application DTOs).
- Domain entities must not be public-constructable (factory method pattern).

Failures here indicate a layer boundary violation that slipped through code review.

### Integration Tests — `CleanArchitecture.IntegrationTests`

Spins up the full ASP.NET Core pipeline in-process via `WebApplicationFactory<Program>`.
Uses **EF Core InMemory** provider — no real SQL Server required to run tests.

| Test class | Scenarios covered |
|---|---|
| `ProductsEndpointTests` | GET all, POST valid/invalid, GET by non-existent ID |
| `ProductsMutationTests` | PUT valid/404/422, DELETE 204/404, soft delete hides record, **cache invalidation E2E** |

**Shared factory via `ICollectionFixture`**: both test classes share a single `WebApplicationFactory` instance (defined in `IntegrationTestCollection`). This prevents Serilog's `ReloadableLogger` from being frozen twice when xUnit runs parallel test classes.

### Why EF InMemory instead of Testcontainers + SQL Server

| | EF InMemory | Testcontainers (SQL Server) |
|---|---|---|
| Speed | ⚡ ~4 s suite | 🐢 ~30 s (Docker pull + startup) |
| Docker required | ❌ No | ✅ Yes |
| SQL Server-specific behavior | ❌ Not tested | ✅ Tested |
| Suitable for | CI smoke tests | Full integration / migration tests |

The template uses **EF InMemory** for fast, dependency-free CI. For production projects, see the **Testcontainers** note below.

> **Testcontainers upgrade path**: Replace `AddEntityFrameworkInMemoryDatabase()` in `WebApplicationFactoryFixture` with a Testcontainers `MsSqlContainer`, add `Respawn` to reset the database between tests. This validates real SQL Server behavior including migrations, indexes, and transactions.

## Consequences
- **Positive**: Tests run in ~5 s total with zero external dependencies (no Docker, no SQL Server).
- **Positive**: Each layer is tested in isolation — unit tests don't start the HTTP stack; architecture tests don't execute any code.
- **Positive**: The cache invalidation test (`Update_AfterCacheHit_ShouldReturnFreshData`) demonstrates the full read→mutate→read cycle with real pipeline behaviors running.
- **Negative**: EF InMemory does not enforce referential integrity, unique constraints, or SQL Server-specific column types — these require Testcontainers.
- **Negative**: No load or contract tests. For APIs with external consumers, add Pact (consumer-driven contracts) or k6 (load testing).
