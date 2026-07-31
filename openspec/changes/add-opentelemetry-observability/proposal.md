## Why

Philobiblos currently emits structured logs with correlation IDs, but it has no visibility into request traces, runtime metrics, or service health. This makes it hard to diagnose latency issues, understand throughput, or operate the application in production. Adding OpenTelemetry-based observability closes this gap and aligns the project with modern production-ready practices.

## What Changes

- Add OpenTelemetry to the backend with tracing and metrics exporters.
- Instrument HTTP requests, database calls, and business operations with trace spans.
- Expose a `/health` endpoint that reports application and dependency health (database).
- Expose a `/metrics` endpoint for Prometheus scraping.
- Configure the Docker Compose stack to collect traces and metrics via an OpenTelemetry Collector.
- Update ADRs and README to document the observability setup.

## Capabilities

### New Capabilities

- `distributed-tracing`: Request-level tracing across the ASP.NET Core pipeline and database operations using OpenTelemetry.
- `runtime-metrics`: Application and runtime metrics exposed via a Prometheus-compatible `/metrics` endpoint.
- `health-checks`: A `/health` endpoint that reports overall service health and database connectivity.

### Modified Capabilities

- None.

## Impact

- Backend: new NuGet packages (`OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime`, `Microsoft.Extensions.Diagnostics.HealthChecks`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`).
- Docker Compose: adds OpenTelemetry Collector, Prometheus, and optionally Jaeger/Tempo services.
- No breaking changes to the existing public API contract.
