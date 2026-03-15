# ADR-001: Clean Architecture as the Architectural Style

## Status
Accepted

## Context
We need a maintainable, testable, and scalable architecture that enforces clear separation of concerns and allows us to swap infrastructure concerns (database, external APIs) without affecting business logic.

## Decision
We adopt **Clean Architecture** (by Robert C. Martin) organized in four layers:

1. **Domain** — Core business entities, value objects, domain events, and interfaces (contracts). No external dependencies.
2. **Application** — Orchestrates use cases via CQRS (MediatR). Defines interfaces that Infrastructure implements.
3. **Infrastructure** — Implements persistence (EF Core), external services, and identity (JWT).
4. **Presentation** — HTTP entry point (ASP.NET Core WebAPI). Delegates all work to Application via MediatR.

### Dependency Rule
All source code dependencies must point **inward**:
```
Presentation → Application → Domain
Infrastructure → Application → Domain
```
Infrastructure and Presentation never reference each other.

## Consequences
- **Positive**: Business logic is fully isolated and easily unit-tested without any infrastructure.
- **Positive**: Infrastructure can be swapped (e.g., change SQL Server to PostgreSQL) by only changing Infrastructure.
- **Positive**: Architecture rules are enforced by automated tests (`ArchitectureTests` project using NetArchTest).
- **Negative**: More boilerplate for simple CRUD operations. Mitigated by code generation / templates.
