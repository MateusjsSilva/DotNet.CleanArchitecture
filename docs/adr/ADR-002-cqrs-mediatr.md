# ADR-002: CQRS with MediatR for Use Case Orchestration

## Status
Superseded by [ADR-017: Custom Mediator Implementation](ADR-017-custom-mediator-implementation.md)

## Context
The Application layer needs a consistent way to handle use cases. We want to separate reads (Queries) from writes (Commands) and have a single, clean pipeline for cross-cutting concerns like logging, validation, and caching.

## Decision
~~Use **MediatR 12**~~ → replaced by a zero-dependency custom mediator (see ADR-017). The CQRS contracts and pipeline remain identical in intent.

- **Commands** — mutate state, return a result or void. Named `{Action}{Entity}Command`. Implement `ICommand` or `ICommand<TResponse>`.
- **Queries** — read-only, never mutate state. Named `Get{Entity/Entities}Query`. Implement `IQuery<TResponse>`.
- **Domain Event Handlers** — react to domain events. Named `{Event}Handler`. Implement `IDomainEventHandler<TEvent>`.
- **Pipeline Behaviors** — cross-cutting concerns registered as open-generic `IPipelineBehavior<,>`, executed in order:

  1. `LoggingBehavior` — creates an OpenTelemetry `Activity` per request; logs request name and tags error type on failure.
  2. `ValidationBehavior` — runs FluentValidation validators; throws `ValidationException` (→ HTTP 422) if any rule fails.
  3. `CachingBehavior` — checks `IDistributedCache` if the request implements `ICacheableQuery`; stores result on cache miss. Evicts keys if the request implements `ICacheInvalidator`.

## Naming Conventions

| Type | Pattern | Example |
|---|---|---|
| Command | `{Action}{Entity}Command` | `CreateProductCommand` |
| Query | `Get{Entity}Query` | `GetProductByIdQuery` |
| Handler | `{Command/Query}Handler` | `CreateProductCommandHandler` |
| Event handler | `{Event}Handler` | `ProductCreatedEventHandler` |
| Validator | `{Command}Validator` | `CreateProductCommandValidator` |

## Consequences
- Each use case lives in its own file (Single Responsibility).
- Adding a new use case never touches existing code (Open/Closed).
- Pipeline behaviors apply automatically to all requests with no per-handler wiring.
- Handlers are `internal sealed` — not part of the public API surface.
- Caching is opt-in per query via `ICacheableQuery` marker interface, keeping handlers free of cache concerns.
