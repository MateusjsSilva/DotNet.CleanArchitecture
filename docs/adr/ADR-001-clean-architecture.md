# ADR-001: Clean Architecture as the Architectural Style

## Status
Accepted

## Context
We need a maintainable, testable, and scalable architecture that enforces clear separation of concerns and allows us to swap infrastructure concerns (database, external APIs) without affecting business logic.

## Decision
We adopt **Clean Architecture** (by Robert C. Martin) organized in four layers plus a modules layer:

1. **Domain** — Core business entities, value objects, domain events, repository interfaces, and exceptions. Zero external dependencies.
2. **Application** — Orchestrates use cases via CQRS (MediatR). Defines interfaces that Infrastructure implements (`IProductRepository`, `ICurrentUserService`, `IAuthService`, etc.).
3. **Infrastructure** — Implements persistence (EF Core + Dapper), identity (ASP.NET Core Identity + JWT), outbox processor, and other external concerns.
4. **WebAPI** — HTTP entry point (ASP.NET Core). Controllers delegate all work to Application via MediatR. Owns middleware, rate limiting, CORS, and observability wiring.
5. **Modules/AI** — Optional pluggable module (Semantic Kernel). Isolated so it can be removed without affecting other layers.

### Dependency Rule

```
WebAPI      → Application → Domain
WebAPI      → Infrastructure
WebAPI      → Modules/AI
Infrastructure → Application → Domain
```

Infrastructure and WebAPI never reference each other. Domain never references anything.

### Enforcement

Layer dependency rules are validated automatically on every build via the `ArchitectureTests` project (NetArchTest). A failing test means a layer boundary was crossed.

## Consequences
- **Positive**: Business logic is fully isolated and unit-testable without any infrastructure.
- **Positive**: Infrastructure can be swapped (e.g., SQL Server → PostgreSQL) by only changing the Infrastructure project.
- **Positive**: Architecture rules are machine-enforced, not just a convention.
- **Negative**: More files/boilerplate for simple CRUD. Mitigated by code generation and the `dotnet new` template.
