# CLAUDE.md — Project Context for AI Assistants

This file is loaded automatically by Claude Code and similar AI tools.
It gives you the architectural contracts you must follow when generating or modifying code in this repository.

---

## What this project is

A **production-ready .NET 10 Clean Architecture template** (`dotnet new cleanarch`).
Every pattern implemented here is deliberate and documented in `docs/adr/`.
Do not simplify, replace, or work around patterns without reading the relevant ADR first.

---

## Layer map — dependency rules (enforced by ArchitectureTests)

```
Domain  ←  Application  ←  Infrastructure
                        ←  WebAPI
```

- **Domain**: zero external dependencies. Entities, domain events, repository interfaces, value objects, exceptions.
- **Application**: depends only on Domain. Use cases (CQRS), pipeline behaviors, DTOs, validators, interfaces.
- **Infrastructure**: depends on Domain + Application. EF Core, repositories, identity, outbox, caching wiring.
- **WebAPI**: depends on Application + Infrastructure. Controllers, middlewares, extensions.
- **Modules/AI**: optional plug-in module. WebAPI depends on it; no other layer does.

**Never** add a reference from an inner layer to an outer layer.

---

## CQRS contracts — always use these

```csharp
// Command with response
public sealed record MyCommand(...) : ICommand<MyDto>, ICacheInvalidator { ... }

// Command without response
public sealed record MyCommand(...) : ICommand { }

// Query (must be cacheable if result changes infrequently)
public sealed record GetMyEntityQuery(Guid Id) : IQuery<MyDto>, ICacheableQuery
{
    public string CacheKey => CacheKeys.MyEntity(Id);
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(10);
    public TimeSpan? SlidingExpiration => null;
}

// Handlers are always internal sealed
internal sealed class MyCommandHandler(IMyRepository repo, IUnitOfWork uow)
    : ICommandHandler<MyCommand, MyDto> { ... }

internal sealed class GetMyEntityQueryHandler(IMyRepository repo)
    : IQueryHandler<GetMyEntityQuery, MyDto> { ... }
```

Handlers are registered via open-generic scanning in `Application/DependencyInjection.cs` — no manual registration needed.

---

## Domain entity rules

```csharp
public sealed class MyEntity : AuditableEntity   // inherit AuditableEntity, NOT BaseEntity directly
{
    // All setters private — state changes only via methods
    public string Name { get; private set; } = string.Empty;

    private MyEntity() { }  // required for EF Core

    // Factory method raises domain event
    public static MyEntity Create(string name)
    {
        var entity = new MyEntity { Name = name };
        entity.RaiseDomainEvent(new MyEntityCreatedEvent(entity.Id, name));
        return entity;
    }
}
```

- Inherit `AuditableEntity` for full audit trail + soft delete automatically.
- Inherit `BaseEntity` only for entities that don't need audit or soft delete.
- Private constructor is **required** for EF Core.
- `Id` is `Guid` (v7, time-ordered) — set by `BaseEntity`, never assign manually.
- Raise domain events from entity methods, not from handlers.

---

## Adding a new feature — step-by-step

### 1. Domain layer

```
src/Domain/Entities/MyEntity.cs          ← entity + factory method + domain events
src/Domain/Events/MyEntityCreatedEvent.cs
src/Domain/Interfaces/IMyEntityRepository.cs
```

```csharp
// IMyEntityRepository.cs
public interface IMyEntityRepository : IRepository<MyEntity>
{
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
```

### 2. Application layer

```
src/Application/DTOs/MyEntityDto.cs
src/Application/UseCases/MyEntities/MyEntityMappings.cs       ← ToDto() extension
src/Application/UseCases/MyEntities/Commands/CreateMyEntity/CreateMyEntityCommand.cs
src/Application/UseCases/MyEntities/Commands/CreateMyEntity/CreateMyEntityCommandHandler.cs
src/Application/UseCases/MyEntities/Queries/GetMyEntityById/GetMyEntityByIdQuery.cs
src/Application/UseCases/MyEntities/Queries/GetMyEntityById/GetMyEntityByIdQueryHandler.cs
src/Application/UseCases/MyEntities/Events/MyEntityCreatedEventHandler.cs
src/Application/UseCases/MyEntities/Common/MyEntityCacheInvalidation.cs
src/Application/Validators/CreateMyEntityCommandValidator.cs
```

**Cache keys** — add to `CacheKeys.cs`:
```csharp
private const string MyEntityEntityPrefix = "entity:myentity";
private const string MyEntityCollectionPrefix = "collection:myentities";
public static string MyEntity(Guid id) => $"{MyEntityEntityPrefix}:{id}";
public static string MyEntityCollections => MyEntityCollectionPrefix;
```

**Cache invalidation** — add `ICacheInvalidator` to mutating commands:
```csharp
public sealed record CreateMyEntityCommand(...) : ICommand<MyEntityDto>, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate => [];
    public IEnumerable<string> CacheKeyPrefixesToInvalidate => MyEntityCacheInvalidation.GetListPrefixes();
}
```

**Event handler** — only metrics + logging, never cache or audit:
```csharp
internal sealed class MyEntityCreatedEventHandler(ILogger<...> logger)
    : IDomainEventHandler<MyEntityCreatedEvent>
{
    public Task Handle(MyEntityCreatedEvent e, CancellationToken ct)
    {
        logger.LogInformation("MyEntity created: {Id}", e.EntityId);
        MyEntityTelemetry.CreatedCounter.Add(1);
        return Task.CompletedTask;
    }
}
```

### 3. Infrastructure layer

```
src/Infrastructure/Persistence/Configurations/MyEntityConfiguration.cs
src/Infrastructure/Persistence/Repositories/MyEntityRepository.cs
```

Register in `Infrastructure/DependencyInjection.cs`:
```csharp
services.AddScoped<IMyEntityRepository, MyEntityRepository>();
```

Add `DbSet` to `ApplicationDbContext`:
```csharp
public DbSet<MyEntity> MyEntities => Set<MyEntity>();
```

Generate migration:
```bash
dotnet ef migrations add AddMyEntity \
  --project src/Infrastructure \
  --startup-project src/WebAPI \
  --output-dir Persistence/Migrations
```

### 4. WebAPI layer

```
src/WebAPI/Controllers/MyEntitiesController.cs
```

```csharp
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/my-entities")]
public sealed class MyEntitiesController(IMediator mediator) : ControllerBase { ... }
```

---

## What event handlers must NOT do

| Concern | Correct component | Wrong |
|---|---|---|
| Cache invalidation | `CachingBehavior` via `ICacheInvalidator` on the command | ~~event handler~~ |
| Audit trail | `ApplicationDbContext.SetAuditFields()` | ~~event handler~~ |
| SaveChanges | `IUnitOfWork` in the command handler | ~~event handler~~ |

Event handlers are for: metrics counters, operational logging, and external notifications.

---

## Persistence rules

- **Write side**: EF Core via `IUnitOfWork.SaveChangesAsync()`. Never call `dbContext.SaveChangesAsync()` directly from handlers — use `IUnitOfWork`.
- **Read side** (complex projections): Dapper via `ISqlConnectionFactory`. See `ProductQueries.cs` for example.
- **Soft delete**: call `entity.SoftDelete()` then `repository.Update(entity)`. Never `repository.Remove()` for soft-deletable entities.
- **Optimistic concurrency**: `AuditableEntity` includes `RowVersion` for products. Include `[FromHeader(Name = "If-Match")]` on PUT/PATCH endpoints to propagate the ETag. Catch `ConcurrencyException` in the middleware.

---

## Validation rules

All validators live in `src/Application/Validators/` and inherit `AbstractValidator<TCommand>`.
Common rules are in `src/Application/Validators/Common/` as extension methods:

```csharp
RuleFor(x => x.Name).ValidateProductName();
```

Duplicate-name checks **must** use `MustAsync` with the repository, guarded by `.When(x => !string.IsNullOrWhiteSpace(x.Name))` to avoid unnecessary DB calls.

---

## Testing rules

| Layer | Location | DB |
|---|---|---|
| Domain logic | `tests/UnitTests/Domain/` | None (pure) |
| Pipeline behaviors | `tests/UnitTests/Behaviors/` | None (mocked) |
| API endpoints | `tests/IntegrationTests/` | SQL Server (Testcontainers) or InMemory fallback |
| Architecture rules | `tests/ArchitectureTests/` | None |

- Integration tests use `WebApplicationFactoryFixture` — never create a second factory.
- Call `await _fixture.ResetDatabaseAsync()` at the start of each test class that mutates data.
- Authenticate via `_fixture.CreateAuthenticatedClient()` — the `TestAuthHandler` auto-authenticates.

---

## Key files to read before modifying

| Task | Files to read first |
|---|---|
| Add entity | `Domain/Common/BaseEntity.cs`, `Domain/Common/AuditableEntity.cs` |
| Add command | Any existing command + handler pair in `UseCases/Products/Commands/` |
| Add query | Any existing query + handler in `UseCases/Products/Queries/` |
| Add caching | `Application/Behaviors/CachingBehavior.cs`, `Application/Common/CacheKeys.cs` |
| Add validation | `Application/Validators/CreateProductCommandValidator.cs` |
| Add event handler | `UseCases/Products/Events/ProductCreatedEventHandler.cs` |
| Touch persistence | `Infrastructure/Persistence/ApplicationDbContext.cs` |
| Add endpoint | `WebAPI/Controllers/ProductsController.cs` |
