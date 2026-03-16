# ADR-008: Observability with OpenTelemetry, Jaeger, Prometheus, and Grafana

## Status
Accepted

## Context
Production systems need distributed tracing, metrics, and structured logging to diagnose issues and understand system behavior. We want a vendor-neutral solution that can export to multiple backends.

## Decision
Use **OpenTelemetry** as the single instrumentation layer with pluggable exporters.

### Signals

| Signal | Library | Exporter |
|---|---|---|
| Traces | OTel AspNetCore + HttpClient + custom `ActivitySource` | OTLP → **Jaeger** (or any OTLP backend) |
| Metrics | OTel AspNetCore + HttpClient + Runtime | OTLP + **Prometheus** scrape (`/metrics`) |
| Logs | **Serilog** | Console + rolling file (`logs/log-*.txt`) |

### Application layer tracing

`ApplicationActivitySource` in the Application layer uses only `System.Diagnostics.ActivitySource` (BCL — no NuGet OTel package). This keeps the Application layer free of infrastructure dependencies. `LoggingBehavior` starts an activity per MediatR request and tags it with error info on failure.

```csharp
// Application layer — no OTel package reference
public static class ApplicationActivitySource
{
    public const string Name = "CleanArchitecture.Application";
    public static readonly ActivitySource Instance = new(Name);
}
```

### Configuration

```json
"Observability": {
  "ServiceName": "CleanArchitecture.API",
  "Otlp": { "Endpoint": "" }
}
```

- Empty endpoint → traces to console, metrics at `/metrics` only.
- Set endpoint → OTLP export to Jaeger/Grafana Tempo + metrics also via OTLP.

### Docker Compose services

- **Jaeger** (`localhost:16686`) — distributed traces UI.
- **Prometheus** (`localhost:9090`) — scrapes `/metrics` every 15 s.
- **Grafana** (`localhost:3000`) — dashboards backed by Prometheus. Pre-provisioned datasource.

## Consequences
- **Positive**: Vendor-neutral — swap Jaeger for Grafana Tempo, or Prometheus for InfluxDB, by changing config only.
- **Positive**: Every HTTP request and every MediatR handler has a correlated trace via `traceId` in the Problem Details error response.
- **Positive**: Application layer has zero OTel package dependency.
- **Negative**: Adds several NuGet packages to the WebAPI project.
