## Context

The current backend is a single `Philobiblos.Api` project using vertical slices:

- `Domain/` holds `Author`, `Book`, `Genre`, `IEntity`.
- `Data/` holds `LibraryDbContext`, EF configurations, and migrations.
- `Infrastructure/` holds cross-cutting concerns (paging, `ProblemDetails`, validation filter, exception middleware).
- `Features/<Entity>/` holds each use case as a static class with endpoint registration, DTOs, validators, and handler that directly calls `LibraryDbContext`.
- `Program.cs` registers `LibraryDbContext`, validators, and maps each endpoint extension.

Handlers contain both business rules (e.g., uniqueness checks, in-use checks, ISBN validation) and persistence details (e.g., `AsNoTracking`, `Include`, `ProjectToResponse`). Unit tests pass an in-memory `LibraryDbContext` to the handlers directly; integration tests hit the HTTP boundary.

## Goals / Non-Goals

**Goals:**
- Separate domain, application, infrastructure, and presentation layers so that domain and application depend only on abstractions.
- Introduce repository interfaces (`IAuthorRepository`, `IBookRepository`, `IGenreRepository`) and an `IUnitOfWork` boundary so that handlers do not reference `DbContext` directly.
- Make application logic independently testable with fakes/mocks rather than EF Core in-memory.
- Keep the public HTTP contract unchanged.

**Non-Goals:**
- No behavioral changes to endpoints, status codes, or error responses.
- No new authentication, authorization, or domain events.
- No migration to a different database or ORM.

## Decisions

### 1. Layered project structure

Split the single API project into four projects/assemblies:

- `Philobiblos.Domain` — entities, repository interfaces, domain exceptions.
- `Philobiblos.Application` — commands, queries, handlers, DTOs, validators. References only `Domain`.
- `Philobiblos.Infrastructure` — EF Core `DbContext`, repository implementations, migrations, EF configurations. References `Domain` and `Application`.
- `Philobiblos.Api` — minimal-API endpoints, endpoint filters, middleware registration, DI wiring. References `Application` and `Infrastructure`.

Rationale: This enforces the dependency rule. `Application` defines what it needs; `Infrastructure` satisfies those needs.

### 2. Repository pattern and unit of work

Define `IRepository<T>` with common operations (GetByIdAsync, Add, Remove, ListAsync, AnyAsync) plus specialized interfaces per aggregate:

- `IAuthorRepository` adds `IsNameTakenAsync` and `IsAuthorInUseAsync`.
- `IGenreRepository` adds `IsNameTakenAsync` and `IsGenreInUseAsync`.
- `IBookRepository` adds `IsIsbnTakenAsync` and `GetByIdWithDetailsAsync`.

Introduce `IUnitOfWork` with a single `SaveChangesAsync(CancellationToken)` method. Repositories share the same EF Core context instance per unit of work. `IUnitOfWork` is the only abstraction that knows how to persist changes.

Rationale: EF Core already implements a repository pattern for the database, but the application should not depend on it. Wrapping EF Core provides an explicit seam for testing and future persistence swaps.

### 3. Keep EF Core and PostgreSQL in Infrastructure

The application layer references repository abstractions, not `Microsoft.EntityFrameworkCore`. The infrastructure layer uses `Include`, `AsNoTracking`, and projections internally.

Rationale: Persistence details stay in one layer, matching the accepted trade-off in the current ADR.

### 4. Move request/response DTOs and mapping to Application

Application handlers receive commands/queries and return application DTOs. The API layer is responsible for mapping application DTOs to HTTP responses (CreatedAtRoute, Ok, NoContent, ProblemDetails).

Rationale: DTOs are an application-layer concern; the API layer should not own domain-to-response transformation.

### 5. Business-rule validation inside Application handlers

Uniqueness checks (case-insensitive author/genre name, ISBN) and reference checks (author/genre exist when creating a book) move into application handlers. Input validation (length, format) remains in FluentValidation validators that run in the endpoint filter.

Rationale: Some rules need the database to answer (e.g., "is this name already taken?"), so they belong in the application layer. Length/format validation can stay at the boundary.

### 6. No MediatR / no CQRS event bus

Keep the command/query pattern explicit without adding a mediator library. Handlers are invoked as plain classes.

Rationale: Minimizes ceremony for a small refactor. A mediator can be added later if cross-cutting concerns like auditing or caching are needed.

### 7. Test strategy

- Unit tests: mock/fake `IAuthorRepository`, `IBookRepository`, `IGenreRepository`, `IUnitOfWork` and test application handlers.
- Integration tests: keep the existing HTTP-level tests; they will exercise the real infrastructure wiring and still act as the contract safety net.
- Business-rule tests: convert from in-memory `DbContext` to fake repositories or a thin in-memory `IUnitOfWork` implementation.

Rationale: Preserves the current test pyramid while making the unit-test layer faster and independent of EF Core.

## Risks / Trade-offs

- [Risk] Increased file/project count for a small CRUD domain. → Mitigation: keep the refactor scoped; do not add domain events, value objects, or excessive abstractions beyond the four layers.
- [Risk] Repository methods become shallow pass-throughs. → Mitigation: add only meaningful aggregate-specific methods; avoid generic `IQueryable` leaking from repositories.
- [Risk] Existing in-memory business-rule tests need significant rewrites. → Mitigation: create a small in-memory `IUnitOfWork` + repository implementation to support the existing tests with minimal churn.
- [Risk] EF Core migrations and `LibraryDbContext` need to move across projects. → Mitigation: migration history stays in `Infrastructure`; the startup project is the API.
- [Risk] Public API must remain unchanged. → Mitigation: leave integration tests untouched and run them after each layer move.

## Migration Plan

1. Create new projects and update solution file.
2. Move `Domain` entities and add repository interfaces.
3. Move `Infrastructure` (`DbContext`, configs, migrations, middleware, exception types) and implement repositories.
4. Create Application commands/queries and handlers; remove `Features/` static handler classes.
5. Update `Program.cs` to register application services and map endpoints that call Application handlers.
6. Rewrite unit/business-rule tests to use fakes/in-memory repositories.
7. Run full backend test suite (unit + integration) and verify Docker Compose still works.
8. Update ADRs and README architecture section.

## Open Questions

None at this design stage.
