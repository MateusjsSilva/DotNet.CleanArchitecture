# ADR-002: CQRS with MediatR for Use Case Orchestration

## Status
Accepted

## Context
The Application layer needs a consistent way to handle use cases. We want to separate reads (Queries) from writes (Commands) and have a clean pipeline for cross-cutting concerns like validation and logging.

## Decision
Use **MediatR** to implement a lightweight CQRS pattern:
- **Commands** — mutate state, return result or void. Named `{Action}{Entity}Command`.
- **Queries** — read-only, never mutate state. Named `Get{Entity/Entities}Query`.
- **Pipeline Behaviors** — cross-cutting concerns (logging, validation) registered as `IPipelineBehavior<,>`.

## Consequences
- Each use case is in its own file (Single Responsibility).
- Adding a new use case does not touch existing code (Open/Closed).
- Pipeline behaviors automatically apply to all requests.
- Handlers are `internal sealed` — not part of the public API.
