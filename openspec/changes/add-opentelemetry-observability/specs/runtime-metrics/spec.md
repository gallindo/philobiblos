## Purpose

Expose runtime and application metrics from the Philobiblos backend in a Prometheus-compatible format so that operators can monitor throughput, latency, errors, and resource usage.

## ADDED Requirements

### Requirement: Metrics endpoint is exposed
The system SHALL expose a `/metrics` endpoint that returns metrics in Prometheus exposition format.

#### Scenario: Prometheus scrapes metrics
- **WHEN** a GET request is made to `/metrics`
- **THEN** the response has `Content-Type: text/plain; version=0.0.4; charset=utf-8`
- **AND** the response body contains valid Prometheus exposition format data

### Requirement: HTTP request metrics are collected
The system SHALL collect counters and histograms for HTTP requests, including request count, duration, and response status code.

#### Scenario: API request increments counters
- **WHEN** an HTTP request to `/api/*` completes
- **THEN** a request counter is incremented with labels for method, route, and status code
- **AND** a histogram records the request duration in seconds

### Requirement: Runtime metrics are collected
The system SHALL collect .NET runtime metrics such as memory usage, GC collections, and thread-pool thread count.

#### Scenario: Runtime metrics are present
- **WHEN** `/metrics` is scraped
- **THEN** the response contains runtime metrics (e.g., `process_memory_bytes`, `dotnet_gc_collection_count`)

### Requirement: Custom business metrics are supported
The system SHALL allow handlers to record custom counters or histograms for domain events (e.g., entities created).

#### Scenario: Entity creation is counted
- **WHEN** a genre, author, or book is created successfully
- **THEN** a counter labeled by entity type is incremented
