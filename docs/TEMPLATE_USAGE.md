# Template Usage

This template is meant to generate a clean production-ready API shell by default.
The sample Products feature is optional and should be used as a reference, not as mandatory starter code.

## Recommended Start

```bash
dotnet new install .
dotnet new cleanarch -n MyCompany.MyApp
cd MyCompany.MyApp
dotnet build
```

The generated project includes authentication, Identity, PostgreSQL wiring, outbox tables, observability, health checks, rate limiting, API versioning, and test projects. It does not include a business domain entity until you add one.

After adding your first entity and EF configuration, create the first migration:

```bash
dotnet ef migrations add InitialCreate \
  --project src/Infrastructure \
  --startup-project src/WebAPI \
  --output-dir Persistence/Migrations
```

## Options

| Option | Default | Purpose |
|---|---:|---|
| `--Framework net10.0|net9.0` | `net10.0` | Target framework replacement across projects. |
| `--UseDocker` | `true` | Includes Dockerfile, compose, PostgreSQL, Redis, Jaeger, Prometheus, and Grafana. |
| `--IncludeSample` | `false` | Includes the Products sample domain, endpoints, tests, seed data, and sample migrations. |
| `--UseRedis` | `false` | Keeps cache wiring ready for Redis; set a Redis connection string to use it. |
| `--IncludeAIModule` | `true` | Includes the optional Semantic Kernel module and AI controller. |

Examples:

```bash
# Clean project for real application work
dotnet new cleanarch -n Billing.Api

# Learning/demo project with full Products sample
dotnet new cleanarch -n CleanArchDemo --IncludeSample

# API without Docker files
dotnet new cleanarch -n Internal.Api --UseDocker false

# API without the AI module
dotnet new cleanarch -n Core.Api --IncludeAIModule false
```

## First Feature Checklist

Add features in layer order:

1. Domain: entity, domain events, repository interface.
2. Application: DTOs, commands, queries, handlers, validators, cache keys.
3. Infrastructure: EF configuration, repository implementation, DI registration, migration.
4. WebAPI: controller and response metadata.
5. Tests: domain/unit tests, integration tests, architecture tests if a new rule is needed.

Do not start by copying the Products sample into a real feature. Use it as a reference for patterns, then name the real domain clearly.

## When To Use `--IncludeSample`

Use it when:

- You are learning the architecture.
- You want executable examples of CQRS, validation, caching, Dapper projections, soft delete, optimistic concurrency, outbox, and endpoint tests.
- You are updating template internals and need a full sample feature for regression tests.

Avoid it when:

- You are starting a real product.
- You already know the domain you need to model.
- You would delete or rename Products immediately after generation.

