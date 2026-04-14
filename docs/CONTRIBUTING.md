# Contributing Guide

## Who should use this template

| You want... | Use this template? |
|---|---|
| A production-ready starting point for a .NET 10 API | Yes |
| To learn Clean Architecture patterns with working examples | Yes |
| A minimal/micro-service scaffold with no opinions | No — too much structure |
| A Blazor or gRPC project | No — REST/HTTP only |

---

## Quick Start (after `dotnet new cleanarch -n MyApp`)

```bash
# 1. Delete the template migrations and create your own
rm -r src/Infrastructure/Persistence/Migrations
dotnet ef migrations add InitialCreate \
  --project src/Infrastructure --startup-project src/WebAPI \
  --output-dir Persistence/Migrations

# 2. Set secrets (never put real values in appsettings.json)
dotnet user-secrets set "JwtSettings:Secret" "$(openssl rand -base64 48)" \
  --project src/WebAPI

# 3. Apply migrations and run
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
dotnet run --project src/WebAPI
```

---

## Adding a new feature

Follow the layer order below. Each step adds only what that layer owns.

### Step 1 — Domain

Create the entity, events, and repository interface. No EF Core, no HTTP, no DI here.

```
src/Domain/Entities/Order.cs
src/Domain/Events/OrderPlacedEvent.cs
src/Domain/Interfaces/IOrderRepository.cs
```

**Entity checklist**
- [ ] Inherits `AuditableEntity` (gives audit trail + soft delete for free)
- [ ] Private parameterless constructor for EF Core
- [ ] All property setters are `private`
- [ ] State-changing methods raise domain events via `RaiseDomainEvent(...)`
- [ ] Factory method (`Create(...)`) validates invariants with `ArgumentException` / `ArgumentOutOfRangeException`

```csharp
public sealed class Order : AuditableEntity
{
    public string Reference { get; private set; } = string.Empty;
    public decimal Total { get; private set; }

    private Order() { }

    public static Order Place(string reference, decimal total)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(total);

        var order = new Order { Reference = reference, Total = total };
        order.RaiseDomainEvent(new OrderPlacedEvent(order.Id, reference));
        return order;
    }
}
```

---

### Step 2 — Application

Create the DTO, mappings, command/query/handler, validator, event handler, and cache helpers.

```
src/Application/DTOs/OrderDto.cs
src/Application/UseCases/Orders/OrderMappings.cs
src/Application/UseCases/Orders/Commands/PlaceOrder/PlaceOrderCommand.cs
src/Application/UseCases/Orders/Commands/PlaceOrder/PlaceOrderCommandHandler.cs
src/Application/UseCases/Orders/Queries/GetOrderById/GetOrderByIdQuery.cs
src/Application/UseCases/Orders/Queries/GetOrderById/GetOrderByIdQueryHandler.cs
src/Application/UseCases/Orders/Events/OrderPlacedEventHandler.cs
src/Application/UseCases/Orders/Common/OrderCacheInvalidation.cs
src/Application/Validators/PlaceOrderCommandValidator.cs
```

**Command checklist**
- [ ] `public sealed record` (immutable, value-based equality)
- [ ] Implements `ICommand<TDto>` (with response) or `ICommand` (void)
- [ ] Implements `ICacheInvalidator` if it mutates data read by a cached query
- [ ] No infrastructure references — only domain interfaces

**Query checklist**
- [ ] Implements `IQuery<TResponse>`
- [ ] Implements `ICacheableQuery` with a stable `CacheKey` and `AbsoluteExpiration`
- [ ] `SlidingExpiration` is `null` for data that can go stale (use absolute instead)

**Handler checklist**
- [ ] `internal sealed class` (not public — enforced by ArchitectureTests)
- [ ] No `SaveChangesAsync` in query handlers
- [ ] Command handlers call `await unitOfWork.SaveChangesAsync(ct)` — never `dbContext.SaveChangesAsync()`
- [ ] Throws `NotFoundException` for missing entities (maps to HTTP 404 via middleware)

**Event handler checklist**
- [ ] `internal sealed class`
- [ ] Only logs and increments metrics counters
- [ ] Never touches cache, audit fields, or calls `SaveChangesAsync`

**Validator checklist**
- [ ] `public sealed class` inheriting `AbstractValidator<TCommand>`
- [ ] Async DB checks (`MustAsync`) guarded by `.When(x => !string.IsNullOrWhiteSpace(x.Field))`
- [ ] Reuse common rules from `Validators/Common/` where they exist

**Cache key checklist**
- [ ] Add entity key + collection prefix to `CacheKeys.cs`
- [ ] Add `OrderCacheInvalidation` static class (mirrors `ProductCacheInvalidation`)

---

### Step 3 — Infrastructure

```
src/Infrastructure/Persistence/Configurations/OrderConfiguration.cs
src/Infrastructure/Persistence/Repositories/OrderRepository.cs
```

Register in `Infrastructure/DependencyInjection.cs`:
```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
```

Add `DbSet` to `ApplicationDbContext.cs`:
```csharp
public DbSet<Order> Orders => Set<Order>();
```

Create migration:
```bash
dotnet ef migrations add AddOrders \
  --project src/Infrastructure --startup-project src/WebAPI \
  --output-dir Persistence/Migrations
```

**Configuration checklist**
- [ ] Primary key via `builder.HasKey()`
- [ ] Required string fields with `.HasMaxLength()`
- [ ] Decimal fields with `.HasPrecision(18, 2)`
- [ ] `RowVersion` if optimistic concurrency is needed: `builder.Property(p => p.RowVersion).IsRowVersion()`
- [ ] Indexes for columns used in `WHERE` clauses

**Repository checklist**
- [ ] `internal sealed class` extending `BaseRepository<Order>`
- [ ] `AsNoTracking()` on all read methods
- [ ] No `SaveChangesAsync` — that belongs to the command handler via `IUnitOfWork`

---

### Step 4 — WebAPI

```
src/WebAPI/Controllers/OrdersController.cs
```

**Controller checklist**
- [ ] `[ApiVersion(1)]` and `[Route("api/v{version:apiVersion}/orders")]`
- [ ] `public sealed class` extending `ControllerBase` (not `Controller`)
- [ ] `[Authorize]` at class level; `[AllowAnonymous]` on individual public endpoints
- [ ] `[ProducesResponseType<ApiResponse<T>>(StatusCodes.Status200OK)]` on every action
- [ ] Return `ApiResponse<T>` wrapper on success: `Ok(new ApiResponse<T>(result))`
- [ ] Use `CreatedAtAction` for POST: `CreatedAtAction(nameof(GetById), new { id = result.Id }, ...)`
- [ ] For PUT/PATCH: bind route `id` and use `command with { Id = id }` to merge

---

### Step 5 — Tests

```
tests/UnitTests/Domain/OrderTests.cs          ← entity invariants
tests/IntegrationTests/Orders/OrderEndpointTests.cs
```

**Unit test checklist**
- [ ] Test `Create()`/`Place()` happy path
- [ ] Test each guard (`ArgumentException`, `ArgumentOutOfRangeException`)
- [ ] Test domain event is raised (check `entity.DomainEvents`)
- [ ] Test `SoftDelete()` idempotency if applicable

**Integration test checklist**
- [ ] Use `_fixture.CreateAuthenticatedClient()` — never create a raw `HttpClient`
- [ ] Call `await _fixture.ResetDatabaseAsync()` in constructor (`IAsyncLifetime.InitializeAsync`)
- [ ] Test happy path end-to-end (POST → GET → assert DTO)
- [ ] Test 404 for unknown ID
- [ ] Test 422 for invalid input (empty name, negative price, etc.)

---

## Patterns and where NOT to put things

| Concern | Right place | Wrong places |
|---|---|---|
| Cache invalidation | `CachingBehavior` via `ICacheInvalidator` on the command | event handler, repository |
| Audit stamps (CreatedAt/By, etc.) | `ApplicationDbContext.SetAuditFields()` | entity methods, event handlers |
| Domain events → Outbox | `ApplicationDbContext.ConvertDomainEventsToOutboxMessages()` | anywhere else |
| Business validation | `AbstractValidator<T>` in `Application/Validators/` | domain entity, controller, handler |
| Invariant guards (null, range) | Domain entity constructor / factory method | validators, controllers |
| SaveChanges | `IUnitOfWork.SaveChangesAsync()` in command handler | query handler, event handler, repository |
| Complex read projections | Dapper via `ISqlConnectionFactory` | EF Core LINQ with `.Include()` chains |

---

## Code review checklist

Before opening a PR, verify:

- [ ] No inner layer references an outer layer (run `dotnet test tests/ArchitectureTests`)
- [ ] All handlers are `internal sealed`
- [ ] No `SaveChangesAsync` in query handlers or event handlers
- [ ] New entity inherits `AuditableEntity` (not raw `BaseEntity`) unless intentional
- [ ] Validators cover null/empty, length limits, and uniqueness where applicable
- [ ] Integration tests cover at least the happy path and one error case
- [ ] New `DbSet` registered and migration created
- [ ] New repository registered in `DependencyInjection.cs`
- [ ] No real secrets committed (`appsettings.json` placeholders only)
- [ ] Cache keys added to `CacheKeys.cs` if the feature adds cacheable queries
- [ ] `dotnet build` and `dotnet test` pass locally

---

## Reporting bugs

Open an issue at the repository URL with:
1. .NET version (`dotnet --version`)
2. The exact steps to reproduce
3. Expected vs. actual behavior
4. Relevant log output or exception stack trace
