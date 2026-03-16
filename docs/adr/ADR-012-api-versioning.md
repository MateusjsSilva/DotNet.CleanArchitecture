# ADR-012: API Versioning via URL Segment

## Status
Accepted

## Context
APIs evolve over time. Without versioning, breaking changes require all consumers to update simultaneously. We need a versioning strategy that is explicit, easy to discover, and compatible with OpenAPI/Scalar documentation.

## Decision
Use **URL segment versioning** via `Asp.Versioning.Mvc`.

```
GET /api/v1/products
GET /api/v2/products   ← future breaking change
```

### Configuration

```csharp
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
```

`ReportApiVersions = true` adds `api-supported-versions` and `api-deprecated-versions` headers to every response, helping consumers discover available versions.

### Controller declaration

```csharp
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProductsController : ControllerBase { }
```

To introduce a v2, add a new controller (or new methods on the existing controller with `[ApiVersion(2)]`) without touching v1.

### Why URL segment over headers/query string

| Strategy | Discoverability | Cacheable | Browser friendly |
|---|---|---|---|
| URL segment | ✅ explicit | ✅ | ✅ |
| Query string | ✅ | ✅ | ✅ |
| `Accept` header | ❌ hidden | ❌ | ❌ |

URL segment versioning is the most explicit and universally supported strategy.

## Consequences
- **Positive**: Version is visible in every request log, trace, and URL.
- **Positive**: Multiple versions can coexist without routing conflicts.
- **Positive**: `AssumeDefaultVersionWhenUnspecified = true` means existing clients calling `/api/products` still work (resolved to v1).
- **Negative**: URL changes when a new version is introduced (expected and intentional).
