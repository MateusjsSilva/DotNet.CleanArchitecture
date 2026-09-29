# ADR-013: Testing Strategy - Three-Layer Pyramid

## Status
Accepted

## Context

A Clean Architecture template needs to demonstrate how to test each layer in isolation without coupling tests to infrastructure concerns. We need to cover domain rules, application behavior, architectural constraints, and full HTTP request flows.

## Decision

Three test projects target different layers of the pyramid:

```text
Integration Tests   real HTTP + real PostgreSQL via Testcontainers
Unit Tests          pure logic with mocked dependencies
Architecture Tests  static dependency-rule analysis with NetArchTest
```

Total: **101 tests passing**, 3 skipped by design in the sample suite.

### Unit Tests - `CleanArchitecture.UnitTests`

Dependencies: `Application` + `Domain` projects only. No Infrastructure, no WebAPI.

| Area | What is tested |
|---|---|
| `Domain/` | Entity invariants, domain events, soft delete idempotence |
| Feature tests | Command validators and command handlers with NSubstitute mocks |
| `Behaviors/` | Cache miss/hit, cache invalidation, and passthrough for plain requests |

Handlers are tested by mocking repositories and `IUnitOfWork`, verifying calls and return values without touching the database.

### Architecture Tests - `CleanArchitecture.ArchitectureTests`

Enforces Clean Architecture dependency rules using NetArchTest:

- Domain must not reference Application, Infrastructure, or WebAPI.
- Application must not reference Infrastructure or WebAPI.
- Infrastructure must not reference WebAPI.
- Controllers must not reference Domain or Infrastructure directly.
- Use case handlers must be `internal`.
- Validators must reside in the Application layer.
- Commands must not also implement `IQuery`.
- Handlers must not depend on other handlers.

Failures here indicate a layer boundary violation that slipped through code review.

### Integration Tests - `CleanArchitecture.IntegrationTests`

Spins up the full ASP.NET Core pipeline in-process via `WebApplicationFactory<Program>`. Uses **Testcontainers** (`Testcontainers.PostgreSql`) with a real PostgreSQL container, reset between test classes via `Respawn`.

| Test class | Scenarios covered |
|---|---|
| Feature query tests | Pagination, filtering, ordering, lookup by ID, summaries |
| Feature mutation tests | POST, PUT, PATCH, DELETE, validation, 404, 409, soft delete, cache invalidation |
| `RateLimitingTests` | 429 enforcement and Problem Details format; skipped in CI/Test where appropriate |

**Shared factory via `ICollectionFixture`**: test classes share a single `WebApplicationFactory` + `PostgreSqlContainer` instance. This prevents duplicate host setup problems and keeps the suite fast enough for local feedback.

**Authentication**: integration tests use a `TestAuthHandler` that auto-authenticates requests under the `Test` scheme. `CreateAuthenticatedClient()` returns a pre-configured `HttpClient`.

### Why Testcontainers instead of EF InMemory

| | EF InMemory | Testcontainers (PostgreSQL) |
|---|---|---|
| Speed | Faster | Slower, Docker startup required |
| Docker required | No | Yes |
| Provider-specific behavior | Not tested | Tested |
| Unique constraints | Not enforced | Enforced |
| Optimistic concurrency | Not representative | Exercised against the provider |
| Migrations validated | No | Yes |

Testcontainers is used because this template depends on real relational database behavior: migrations, unique constraints, transactions, global query filters, and optimistic concurrency. EF InMemory does not validate those behaviors.

## Consequences

- **Positive**: Tests run against real PostgreSQL, so unique constraints, optimistic concurrency, and EF migrations are exercised.
- **Positive**: Each layer is tested in isolation.
- **Positive**: Cache invalidation tests demonstrate the full read-mutate-read cycle with real pipeline behaviors.
- **Positive**: Rate limiting tests document expected behavior without blocking CI where they are intentionally skipped.
- **Negative**: Integration tests require Docker. CI pipelines without Docker support must use a different runner or skip integration tests.
- **Negative**: No load or contract tests. APIs with external consumers should add Pact or k6.
