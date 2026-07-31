## 1. Infrastructure and dependencies

- [x] 1.1 Add OpenTelemetry NuGet packages to `Philobiblos.Infrastructure`:
  - `OpenTelemetry`
  - `OpenTelemetry.Exporter.OpenTelemetryProtocol`
  - `OpenTelemetry.Exporter.Prometheus.AspNetCore`
  - `OpenTelemetry.Instrumentation.AspNetCore`
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore`
  - `OpenTelemetry.Instrumentation.Http`
  - `OpenTelemetry.Instrumentation.Runtime`
- [x] 1.2 Add health-check packages to `Philobiblos.Infrastructure`:
  - `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`

## 2. OpenTelemetry registration

- [x] 2.1 Add an `AddObservability` DI extension in `Philobiblos.Infrastructure` that registers:
  - Tracing with ASP.NET Core, HttpClient, EF Core, and runtime instrumentation.
  - Metrics with ASP.NET Core and runtime instrumentation.
  - OTLP exporter for traces and metrics.
  - Resource attributes (`service.name=Philobiblos.Api`, `service.version`, `deployment.environment`).
- [x] 2.2 Add a `UseObservability` application builder extension that:
  - Enables Prometheus scraping endpoint middleware.
  - Maps the `/metrics` endpoint.

## 3. Health checks

- [x] 3.1 Register health checks in `AddObservability` including an EF Core database check.
- [x] 3.2 Map a `/health` endpoint in `Program.cs` that returns detailed health status.
- [x] 3.3 Ensure `/health` bypasses authentication/authorization requirements.

## 4. Correlation with traces

- [x] 4.1 Attach the existing Serilog correlation ID to the current OpenTelemetry activity as a tag/attribute.
- [x] 4.2 Add a small unit/integration test that confirms trace attributes include the correlation ID.

## 5. Docker Compose observability stack

- [x] 5.1 Add an `otel-collector` service with an OTLP receiver and exporters for Prometheus and Jaeger.
- [x] 5.2 Add a `prometheus` service that scrapes the collector metrics endpoint.
- [x] 5.3 Add a `jaeger` service for trace visualization.
- [x] 5.4 Configure the API container with `OTEL_EXPORTER_OTLP_ENDPOINT` pointing to the collector.

## 6. Testing and validation

- [x] 6.1 Add integration tests that assert `/health` returns `Healthy` when the database is up.
- [x] 6.2 Add integration tests that assert `/metrics` returns Prometheus format with HTTP and runtime metrics.
- [x] 6.3 Verify traces appear in Jaeger after running a few API requests.
- [x] 6.4 Verify Prometheus scrapes metrics successfully.

## 7. Documentation

- [x] 7.1 Add ADR 0007 documenting the OpenTelemetry + Prometheus + Jaeger observability choice.
- [x] 7.2 Update README.md with the new `/health` and `/metrics` endpoints and how to run the observability stack.
- [x] 7.3 Update `openspec/changes/add-opentelemetry-observability/tasks.md` as tasks are completed.
