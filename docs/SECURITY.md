# Security and Dependency Checks

Use this checklist before publishing template changes or starting a new production app from the template.

## Dependency Audit

```bash
dotnet restore
dotnet list package --vulnerable --include-transitive
```

The vulnerable package list should be empty. If a vulnerable transitive package appears, prefer updating the top-level package that brings it in. Add a direct package reference only when the upstream package cannot be updated yet.

## Secrets

- Keep `appsettings.json` values as placeholders.
- Use user secrets locally for `JwtSettings:Secret`, AI provider keys, and external service credentials.
- Use environment variables or a secret manager in Docker, CI, and production.
- Never commit `.env`, generated certificates, local database passwords, or real JWT signing keys.

## Template Defaults

- The Products sample is opt-in via `--IncludeSample`.
- The AI module is opt-in via `--IncludeAIModule`.
- PostgreSQL is the default relational provider for Docker, EF Core, Dapper, and integration tests.

## Pre-PR Checks

```bash
dotnet build --no-restore
dotnet test --no-build
dotnet list package --vulnerable --include-transitive
```
