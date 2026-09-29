# ADR-001: Clean Architecture as the Architectural Style

## Status
Accepted

## Context

We need a maintainable, testable, and scalable architecture that enforces clear separation of concerns and allows us to swap infrastructure concerns, such as databases and external APIs, without affecting business logic.

## Decision

We adopt **Clean Architecture** organized in four layers plus an optional modules layer:

1. **Domain** - Core business entities, value objects, domain events, repository interfaces, and exceptions. Zero external dependencies.
2. **Application** - Orchestrates use cases via CQRS and the custom mediator. Defines interfaces that Infrastructure implements, such as `IRepository<T>`, feature repositories, `ICurrentUserService`, and `IAuthService`.
3. **Infrastructure** - Implements persistence with EF Core and Dapper, identity with ASP.NET Core Identity and JWT, outbox processing, caching wiring, and other external concerns.
4. **WebAPI** - HTTP entry point. Controllers delegate work to Application via `IMediator`. Owns middleware, rate limiting, CORS, and observability wiring.
5. **Modules/AI** - Optional pluggable Semantic Kernel module. It can be removed without affecting the other layers.

## Dependency Rule

```text
WebAPI          -> Application -> Domain
WebAPI          -> Infrastructure
WebAPI          -> Modules/AI
Infrastructure  -> Application -> Domain
```

Infrastructure and WebAPI never reference each other. Domain never references anything.

## Enforcement

Layer dependency rules are validated automatically via the `ArchitectureTests` project. A failing test means a layer boundary was crossed.

## Consequences

- **Positive**: Business logic is isolated and unit-testable without infrastructure.
- **Positive**: Infrastructure can be swapped, such as PostgreSQL to another relational provider, by changing the Infrastructure project and provider-specific migrations.
- **Positive**: Architecture rules are machine-enforced, not just a convention.
- **Negative**: More files and boilerplate for simple CRUD. The template mitigates this with consistent feature patterns and optional sample code.
