# CleanArchitecture

A .NET 10 Clean Architecture solution template with CQRS, Domain Events, JWT + Refresh Tokens, Dapper, GUID v7, OpenTelemetry, Semantic Kernel AI, and automated Architecture Tests.

## Structure

```
src/
├── Domain/          # Entities, Value Objects, Domain Events, Interfaces
├── Application/     # Use Cases (CQRS/MediatR), DTOs, Validators, Manual Mappings
├── Infrastructure/  # EF Core, Identity, Repositories, Dapper, JWT, External Services
├── WebAPI/          # Controllers, Middleware, Program.cs
└── Modules/
    └── AI/          # Semantic Kernel integration (OpenAI / Azure OpenAI)

tests/
├── UnitTests/           # Domain & Application logic (NSubstitute + FluentAssertions)
├── IntegrationTests/    # API endpoints (WebApplicationFactory + InMemory DB)
└── ArchitectureTests/   # Dependency rules (NetArchTest)
```

## Tech Stack

| Concern | Library |
|---|---|
| CQRS / Mediator | MediatR 12 |
| Validation | FluentValidation 11 |
| Object Mapping | Manual extension methods |
| ORM (write side) | Entity Framework Core 10 |
| Read-side queries | Dapper 2 |
| Authentication | ASP.NET Core Identity + JWT Bearer |
| AI Integration | Microsoft Semantic Kernel 1.73 |
| Logging | Serilog |
| Observability | OpenTelemetry (traces + metrics) → Jaeger / OTLP |
| API Docs | Scalar + OpenAPI |
| Unit Tests | xUnit + NSubstitute + FluentAssertions |
| Architecture Tests | NetArchTest |

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (or LocalDB for development)

### Using as a `dotnet new` template

```bash
# Install the template
dotnet new install .

# Create a new project (renames all namespaces, assemblies, and files)
dotnet new cleanarch -n MyCompany.MyApp

# Or targeting .NET 9
dotnet new cleanarch -n MyCompany.MyApp --Framework net9.0
```

> `sourceName: "CleanArchitecture"` in `template.json` replaces every occurrence of
> `CleanArchitecture` across namespaces, project names, and config files with the value you pass via `-n`.

### Running locally

```bash
# Restore & build
dotnet build

# Add initial migration (only needed once)
dotnet ef migrations add InitialCreate \
  --project src/Infrastructure \
  --startup-project src/WebAPI \
  --output-dir Persistence/Migrations

# Apply migrations
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/WebAPI

# Run the API
dotnet run --project src/WebAPI

# API docs available at:
# https://localhost:PORT/scalar/v1
```

### Running with Docker

```bash
docker compose up --build
```

Services started:
- **API** → http://localhost:5000
- **Jaeger UI** → http://localhost:16686
- **SQL Server** → localhost:1433

### Running tests

```bash
# All tests
dotnet test

# Only architecture tests
dotnet test tests/ArchitectureTests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Configuring Observability

By default (no Docker), traces are exported to the **console**. To use Jaeger or any OTLP-compatible backend, set:

```json
{
  "Observability": {
    "ServiceName": "CleanArchitecture.API",
    "Otlp": {
      "Endpoint": "http://localhost:4317"
    }
  }
}
```

When running via `docker compose`, this is set automatically via environment variable.

### Configuring AI (optional)

Set `AISettings` in `appsettings.json` or user secrets:

```json
{
  "AISettings": {
    "Provider": "OpenAI",
    "ModelId": "gpt-4o-mini",
    "ApiKey": "sk-..."
  }
}
```

For Azure OpenAI:

```json
{
  "AISettings": {
    "Provider": "AzureOpenAI",
    "DeploymentName": "my-deployment",
    "Endpoint": "https://my-resource.openai.azure.com/",
    "ApiKey": "..."
  }
}
```

> If `ApiKey` is empty, the AI module registers a no-op service and the rest of the API works normally.

## Adding a new Use Case

1. Add entity to `src/Domain/Entities/` with factory method and domain events
2. Add repository interface to `src/Domain/Interfaces/`
3. Create command/query + handler in `src/Application/UseCases/{Feature}/`
4. Add validator in `src/Application/Validators/`
5. Add DTO in `src/Application/DTOs/`
6. Add mapping in `src/Application/UseCases/{Feature}/{Feature}Mappings.cs`
7. Register repository in `src/Infrastructure/DependencyInjection.cs`
8. Implement repository in `src/Infrastructure/Persistence/Repositories/`
9. Add controller action in `src/WebAPI/Controllers/`

## Architecture Rules

Enforced by `ArchitectureTests` at every build:

- `Domain` has **no dependencies** on outer layers
- `Application` does **not** depend on `Infrastructure` or `Presentation`
- `Infrastructure` does **not** depend on `Presentation`
- Use Case handlers are **internal** (not public API)

## Key Design Decisions

- **GUID v7** (`Guid.CreateVersion7()`) for time-ordered, database-friendly IDs
- **Refresh token rotation** — each `/auth/refresh` issues a new pair and revokes the old token
- **Dapper on the read side** — queries use `IProductQueries` (Application interface) backed by raw SQL in Infrastructure
- **`IDesignTimeDbContextFactory`** — no startup project needed for `dotnet ef` CLI commands
- **Domain Events** dispatched inside `SaveChangesAsync` via `IPublisher` (MediatR), keeping Domain free of infrastructure concerns
- **Manual mapping** — no AutoMapper/Mapster; explicit `ToDto()` extension methods per feature keep mapping visible and type-safe
- **OpenTelemetry** — traces and metrics via OTLP; `ActivitySource` in Application layer uses only BCL (`System.Diagnostics`), no OTel package dependency on Application
