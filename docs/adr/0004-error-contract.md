# ADR 0004: Uniform error contract with ProblemDetails

## Context

A senior-grade API needs one coherent error shape across validation failures, business-rule conflicts, missing resources, and unhandled exceptions. Disjoint error formats make client code fragile and leak implementation details.

## Decision

Adopt RFC 7807 `ProblemDetails` for every error response:

- FluentValidation validators run in an endpoint filter and short-circuit to `400` with an `errors` dictionary.
- Custom `NotFoundException` and `ConflictException` are mapped to `404` and `409` by global middleware.
- Unhandled exceptions become `500` ProblemDetails with a `correlationId` taken from `HttpContext.TraceIdentifier`; stack traces never leave the server.

## Consequences

- **Positive:** The Angular client can map one shape to form controls, toast messages, or banners.
- **Positive:** No stack-trace leakage improves security posture.
- **Positive:** Correlation IDs tie user-visible errors to structured server logs.
- **Consequence:** All error paths must be exercised in tests to keep the contract trustworthy.
