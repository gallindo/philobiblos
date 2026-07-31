## Purpose

Provide a health endpoint that reports whether the Philobiblos application and its critical dependencies (the database) are ready to serve traffic.

## ADDED Requirements

### Requirement: Health endpoint reports overall status
The system SHALL expose a `/health` endpoint that returns an overall health status and the status of each registered health check.

#### Scenario: Service is healthy
- **WHEN** a GET request is made to `/health`
- **AND** all dependencies are healthy
- **THEN** the response status is `200 OK`
- **AND** the response body indicates `status: "Healthy"`

#### Scenario: Service is unhealthy
- **WHEN** a GET request is made to `/health`
- **AND** a critical dependency is unhealthy
- **THEN** the response status is `503 Service Unavailable`
- **AND** the response body indicates `status: "Unhealthy"`
- **AND** the failing dependency is identified in the response

### Requirement: Database health is checked
The system SHALL register a health check that verifies the application can connect to the PostgreSQL database.

#### Scenario: Database is reachable
- **WHEN** the database health check executes
- **AND** the database is reachable
- **THEN** the check reports `Healthy`

#### Scenario: Database is unreachable
- **WHEN** the database health check executes
- **AND** the database is unreachable
- **THEN** the check reports `Unhealthy`
- **AND** the response includes a descriptive error message

### Requirement: Health endpoint is anonymous
The system SHALL allow unauthenticated access to `/health` so that load balancers and orchestrators can probe service health.

#### Scenario: Anonymous health probe
- **WHEN** an unauthenticated request is made to `/health`
- **THEN** the response contains the current health status
- **AND** the response is not rejected with `401 Unauthorized`
