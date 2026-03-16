# ADR-010: Problem Details (RFC 9457) for Error Responses

## Status
Accepted

## Context
Error responses need a consistent, machine-readable format. Without a standard, consumers must guess the error shape for each endpoint. RFC 9457 (formerly RFC 7807) defines a standard `application/problem+json` format widely adopted in REST APIs.

## Decision
All error responses use **Problem Details** via a custom `ExceptionHandlingMiddleware`. Every response includes `instance` (request path) and `traceId` for distributed tracing correlation.

### Exception → HTTP mapping

| Exception | HTTP Status | ProblemDetails type |
|---|---|---|
| `NotFoundException` | 404 Not Found | `ProblemDetails` |
| `DomainException` | 400 Bad Request | `ProblemDetails` |
| `ValidationException` (FluentValidation) | 422 Unprocessable Entity | `ValidationProblemDetails` |
| Any other `Exception` | 500 Internal Server Error | `ProblemDetails` |

### Response shape

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Unprocessable Entity",
  "status": 422,
  "instance": "/api/v1/products",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errors": {
    "Name": ["Product name is required."],
    "Price": ["Price must be greater than zero."]
  }
}
```

The `traceId` field matches the OpenTelemetry span ID in Jaeger, allowing direct correlation between an error response and the full request trace.

### Positive responses

All successful responses use `ApiResponse<T>`:

```json
{ "data": { ... }, "message": null }
```

## Consequences
- **Positive**: Consistent, predictable error format for all consumers.
- **Positive**: `traceId` in every error response enables instant lookup in Jaeger.
- **Positive**: Validation errors are structured by field name, not a generic message.
- **Negative**: 5xx errors do not expose internal details to the client (intentional for security); details are in the logs only.
