# ADR 0003: EF Core directly in handlers, no repository or unit-of-work

## Context

A common .NET pattern wraps `DbContext` behind `IRepository<T>` and `IUnitOfWork`. For a small CRUD domain, those interfaces mostly forward calls to `DbContext` and add files without adding testability.

## Decision

Handlers use `LibraryDbContext` directly. Cross-cutting concerns are handled by explicit middleware and endpoint filters rather than a mediator pipeline, so MediatR is also omitted.

## Consequences

- **Positive:** Less abstraction noise. Persistence logic is visible exactly where it is used.
- **Positive:** No pipeline indirection to debug; validation, exception mapping, and logging are explicit.
- **Trade-off accepted:** Handlers couple to EF Core. That coupling is contained per slice, and the integration-test suite pins behavior against a real PostgreSQL database, so the abstraction a repository would provide is not load-bearing.
