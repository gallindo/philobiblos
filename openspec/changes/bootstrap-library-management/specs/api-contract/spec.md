## Purpose

Defines the cross-cutting HTTP contract every API endpoint follows: consistent success semantics, a uniform RFC 7807 error format, and a standard pagination envelope for list endpoints.

## ADDED Requirements

### Requirement: Uniform error responses

All API error responses (4xx and 5xx) SHALL use the RFC 7807 `application/problem+json` content type with `type`, `title`, `status`, and `detail` members. Error responses SHALL NOT expose stack traces, internal class names, or connection strings.

#### Scenario: Error follows problem details shape

- **WHEN** any endpoint produces an error response
- **THEN** the body is `application/problem+json` containing `type`, `title`, `status`, and a human-readable `detail`, with no internal implementation details leaked

### Requirement: Validation error shape

Input validation failures SHALL return `400 Bad Request` as problem details extended with an `errors` member: a dictionary mapping field names to arrays of human-readable messages.

#### Scenario: Multiple validation failures reported together

- **WHEN** a request fails validation on several fields
- **THEN** the response returns `400` with one entry per failing field in `errors`, so the client can display all messages at once

### Requirement: Consistent status code semantics

The API SHALL use status codes consistently: `201` + `Location` header for creation, `200` for successful reads and updates, `204` for successful deletion, `400` for validation failures, `404` for missing resources, `409` for uniqueness and referential conflicts.

#### Scenario: Status codes match semantics

- **WHEN** client performs any create, read, update, delete, or invalid operation
- **THEN** the returned status code follows the documented semantics for that outcome

### Requirement: Global exception handling

Unhandled exceptions SHALL be intercepted by a single global handler that logs the exception with a correlation identifier and returns a generic `500` problem details response containing that identifier.

#### Scenario: Unexpected failure is contained

- **WHEN** an endpoint throws an unhandled exception
- **THEN** the response returns `500` problem details with a correlation identifier, and the server log entry for the exception carries the same identifier

### Requirement: Pagination envelope

List endpoints SHALL accept `page` (1-based, default 1) and `pageSize` (default 20, maximum 100) parameters and SHALL return an envelope containing `items`, `page`, `pageSize`, and `totalCount`.

#### Scenario: Page metadata reflects the full result set

- **WHEN** client requests page 2 with pageSize 10 from a catalog of 25 records
- **THEN** the response contains items 11–20, `page` 2, `pageSize` 10, and `totalCount` 25

#### Scenario: Out-of-range pagination parameters

- **WHEN** client requests `page` less than 1 or `pageSize` less than 1 or greater than 100
- **THEN** the request is rejected with a validation error identifying the offending parameter

### Requirement: Sort parameter whitelist

List endpoints SHALL accept a `sort` parameter limited to each endpoint's documented sortable fields, with an explicit direction. Requests with unsupported sort fields SHALL be rejected.

#### Scenario: Unsupported sort field rejected

- **WHEN** client requests a list sorted by a field that is not in the endpoint's sortable whitelist
- **THEN** the request is rejected with a validation error identifying the `sort` parameter

#### Scenario: Default ordering is deterministic

- **WHEN** client requests a list without a sort parameter
- **THEN** results are returned in the endpoint's documented deterministic default order, stable across repeated requests
