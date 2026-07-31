# ADR 0005: Clean Architecture with repository pattern

> **Status:** Accepted
> **Supersedes:** [ADR 0001: Vertical Slice Architecture](0001-vertical-slices.md), [ADR 0003: EF Core directly in handlers](0003-no-repository-pattern.md)

## Context

After the initial implementation, the backend was organized as vertical slices inside a single API project. While that worked for the original scope, the challenge explicitly asked for a refactoring to **Clean Architecture** with a **repository pattern**. The goals were to:

- Isolate domain logic from EF Core and persistence details.
- Make the core business rules testable with in-memory fakes.
- Keep the public HTTP contract unchanged.

## Decision

Split the backend into four projects with strict dependency direction:

1. **`Philobiblos.Domain`** — entities, exceptions, a shared `PagedList<T>`, and repository interfaces (`IRepository<T>`, `IAuthorRepository`, `IBookRepository`, `IGenreRepository`, `IUnitOfWork`).
2. **`Philobiblos.Application`** — commands, queries, handlers, DTOs, validators, `Result<T>`, and `ICommandHandler`/`IQueryHandler` abstractions. Depends only on `Domain`.
3. **`Philobiblos.Infrastructure`** — EF Core `LibraryDbContext`, migrations, repository implementations, exception middleware, and validation filter. Depends on `Domain` and `Application`.
4. **`Philobiblos.Api`** — thin minimal-API host. Maps routes, registers DI, and delegates to handlers. Depends only on `Application` and `Infrastructure`.

Handlers receive `IAuthorRepository`, `IBookRepository`, `IGenreRepository`, and `IUnitOfWork` rather than `LibraryDbContext` directly. No MediatR is introduced; a lightweight `ICommandHandler` / `IQueryHandler` pair is sufficient.

## Consequences

- **Positive:** Domain and application logic can be unit-tested with a simple in-memory repository harness.
- **Positive:** Persistence concerns are isolated and can be swapped (e.g., another EF Core provider or raw SQL) without touching application code.
- **Positive:** The HTTP contract remains unchanged; integration tests and the Angular frontend continue to work without modification.
- **Trade-off accepted:** More files and projects than the original slice. The small CRUD domain means some interfaces are thin, but the structure provides a clear migration path as business rules grow.

## References

- `docs/backend-clean-architecture-refactoring-report.md` — detailed analysis of the original codebase and the refactoring plan.
- `backend/src/Philobiblos.Domain/Repositories/`
- `backend/src/Philobiblos.Application/`
- `backend/src/Philobiblos.Infrastructure/Repositories/`
