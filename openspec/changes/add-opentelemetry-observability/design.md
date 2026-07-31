## Context

Philobiblos already emits structured Serilog logs with correlation IDs via `ExceptionHandlingMiddleware` and `UseSerilogRequestLogging`. The codebase uses Clean Architecture with minimal API endpoints, EF Core over PostgreSQL, and Docker Compose for local orchestration. There is currently no tracing, metrics, or health-check infrastructure.

## Goals / Non-Goals

**Goals:**
- Add OpenTelemetry-based tracing for HTTP requests, outgoing HTTP calls, and EF Core operations.
- Expose a Prometheus-compatible `/metrics` endpoint with HTTP and runtime metrics.
- Add a `/health` endpoint that checks database connectivity and is accessible without authentication.
- Extend `docker-compose.yml` with an OpenTelemetry Collector, Prometheus, and Jaeger so the telemetry stack is runnable locally.
- Document the observability setup in the README and an ADR.

**Non-Goals:**
- Custom dashboards or alerting rules (Grafana is not required for this change).
- Front-end observability (no RUM or browser traces).
- Distributed context propagation between independent services (Philobiblos is a single service).
- Replacing Serilog; logs remain unchanged.

## Decisions

### Use the OpenTelemetry .NET SDK with OTLP export
**Rationale:** The OpenTelemetry .NET SDK is the idiomatic, vendor-neutral choice for ASP.NET Core. OTLP lets traces and metrics flow through a single collector that can forward to Prometheus, Jaeger, or any backend.
**Alternatives considered:**
- Direct exporter per backend (e.g., Jaeger exporter, Prometheus exporter) — ties the app to specific tools.
- Using only `System.Diagnostics` without OpenTelemetry — requires custom collection and lacks easy exporters.

### Use `OpenTelemetry.Exporter.Prometheus.AspNetCore` for `/metrics`
**Rationale:** It provides an ASP.NET Core middleware that serves Prometheus exposition format directly, avoiding a separate sidecar.
**Alternative considered:** Running the OpenTelemetry Collector with a Prometheus receiver and scraping the app externally — more moving parts for local development.

### Add health checks via `Microsoft.Extensions.Diagnostics.HealthChecks`
**Rationale:** Built-in health checks integrate cleanly with the existing minimal API host and EF Core.
**Alternative considered:** A custom endpoint that runs a raw query — duplicates framework functionality.

### Keep `/health` anonymous
**Rationale:** Load balancers and container orchestrators need to probe health without credentials. The endpoint reveals only aggregate status, no sensitive data.

### Export telemetry to a collector sidecar in Docker Compose
**Rationale:** A collector decouples the app from backend tools and is the standard OpenTelemetry pattern. Locally it will forward traces to Jaeger and metrics to Prometheus.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Increased container startup time and memory usage | Collector, Prometheus, and Jaeger are local-only; production can scale them independently or disable them. |
| Metric cardinality explosion from unbounded route parameters | Use low-cardinality route templates (e.g., `/api/genres/{id}`) and avoid high-cardinality labels. |
| Traces/metrics lost if collector is unreachable | Use the OTLP exporter’s default retry/buffer behavior; local development tolerates brief collector outages. |
| Extra NuGet packages in the infrastructure layer | Packages are infrastructure-only and do not leak into Domain or Application. |

## Migration Plan

1. Add NuGet packages and register OpenTelemetry + health checks in `Program.cs`.
2. Add `/health` and `/metrics` endpoints.
3. Update `docker-compose.yml` with `otel-collector`, `prometheus`, and `jaeger` services.
4. Run the stack and verify traces appear in Jaeger and metrics in Prometheus.
5. Update README and add ADR 0007.

## Open Questions

- None.
