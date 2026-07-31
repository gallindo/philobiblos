# ADR 0007: OpenTelemetry Observability with Prometheus and Jaeger

## Status

Accepted

## Context

Philobiblos already emits structured Serilog logs with correlation IDs, but it has no visibility into request traces, runtime metrics, or service health. In order to operate the application confidently and diagnose latency or throughput issues, we need a lightweight, vendor-neutral observability stack that can run locally via Docker Compose.

Key constraints:

- The backend is an ASP.NET Core 10 minimal API.
- The existing public HTTP contract must remain unchanged.
- `/health` must be accessible to load balancers and orchestrators without authentication.
- Front-end observability, custom dashboards, and alerting are out of scope for this change.
- The solution should avoid tying the application to a specific commercial observability backend.

## Decision

We will instrument the backend with the **OpenTelemetry .NET SDK** and export telemetry through an **OpenTelemetry Collector** that forwards traces to **Jaeger** and metrics to **Prometheus**.

- `Philobiblos.Infrastructure` registers OpenTelemetry tracing and metrics through an `AddObservability` extension.
- Tracing instruments the ASP.NET Core pipeline, outgoing HTTP calls, and EF Core database operations.
- Metrics instrument the ASP.NET Core pipeline and .NET runtime and are exposed in Prometheus exposition format on `/metrics`.
- Health checks are registered with an EF Core database check and exposed on `/health`.
- The existing Serilog correlation ID (`HttpContext.TraceIdentifier`) is attached to the current OpenTelemetry activity as a `correlation.id` tag.
- The Docker Compose stack adds `otel-collector`, `prometheus`, and `jaeger` services with the API configured via `OTEL_EXPORTER_OTLP_ENDPOINT` to send telemetry to the collector.

## Consequences

### Positive

- Vendor-neutral telemetry that can be rerouted to other backends by reconfiguring the collector.
- Minimal API changes: only additive `/health` and `/metrics` endpoints.
- Local trace and metric visualization is available out of the box.
- Correlation IDs bridge logs and traces for easier incident investigation.

### Negative

- Additional containers increase local resource usage and startup time.
- Some OpenTelemetry packages are pre-release (`OpenTelemetry.Exporter.Prometheus.AspNetCore`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`) and may introduce breaking changes before a stable release.
- Metric cardinality must be watched; unbounded route parameters or labels could cause storage issues.

## Alternatives considered

- **Direct exporters per backend** (e.g., Jaeger exporter, Prometheus scraping sidecar): Ties the app to specific tools and duplicates exporter configuration.
- **System.Diagnostics without OpenTelemetry**: Requires custom collection and lacks the ecosystem of instrumentation libraries and exporters.
- **Grafana Tempo or cloud APM**: Adds complexity and external dependencies beyond the local scope of this change.

## Related

- `backend/src/Philobiblos.Infrastructure/Observability/ObservabilityExtensions.cs`
- `backend/src/Philobiblos.Api/Program.cs`
- `docker-compose.yml`
- `otel-collector-config.yml`
- `prometheus.yml`
- `docs/adr/0004-error-contract.md`
