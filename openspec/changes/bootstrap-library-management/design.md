# Design: bootstrap-library-management

## Context

Greenfield repo (see proposal.md — Why). Hard constraints from the challenge: .NET/C# REST API, Angular or React SPA, one of SQL Server/PostgreSQL/MySQL, 3 calendar days, and an evaluation weighted toward *justified* decisions over comprehensive features. The domain is three entities with two many-to-one relationships — deliberately simple, which makes architecture *choice and defense* the actual deliverable.

## Goals / Non-Goals

**Goals:**
- A backend structure where every use case is self-contained, discoverable, and cheap to change (vertical slices).
- One coherent "production-aware fundamentals" story: uniform error contract, validation pipeline, structured logging, containerized run, real-database integration tests.
- Every significant decision captured in an ADR so the README can link instead of prose-dump.

**Non-Goals:**
- No authentication/authorization (documented as future work; half-baked JWT under time pressure is a liability, not a signal).
- No mediator library, no repository/UoW wrappers, no domain-event infrastructure — the domain doesn't justify them (see Decisions).
- No CI pipeline, no OpenTelemetry exporter, no deployment target beyond `docker compose up`.

## Decisions

### D0 — Target framework: .NET 10 (LTS)

The build environment ships .NET SDK 10.0.110; the solution targets `net10.0` with EF Core 10 + Npgsql 10. (.NET 10 is the current LTS; nothing in the design is version-sensitive.)

### D1 — Vertical Slice Architecture over Clean Architecture layers

The API is organized by feature, not by technical layer:

```
backend/src/Philobiblos.Api/
├── Features/
│   ├── Genres/  CreateGenre.cs, ListGenres.cs, GetGenre.cs, UpdateGenre.cs, DeleteGenre.cs
│   ├── Authors/ (same shape)
│   └── Books/   (same shape)
├── Domain/      Genre.cs, Author.cs, Book.cs
├── Data/        LibraryDbContext.cs, configurations, migrations
└── Infrastructure/ ExceptionHandlingMiddleware.cs, validation behavior, logging
```

Each slice file owns its endpoint route, request/response DTOs, validator, and handler logic — everything that changes together lives together.

**Rationale:** with three entities and pure-CRUD use cases, four-project Clean Architecture produces near-empty Domain/Application projects and forces every new field to be edited in five places. Slices keep the change surface minimal and read as "right-sized," which is the stronger senior signal. **Alternatives considered:** (a) Clean Architecture 4-project split — rejected as ceremony without payoff *at this scale*, noted as the evolution path if aggregates/domain events appear; (b) classic controller→service→repository layering — rejected as the weakest signal and the most boilerplate.

### D2 — EF Core directly in slice handlers; no repository pattern, no MediatR

Handlers use `LibraryDbContext` directly. No `IRepository<T>` wrappers (DbContext already is a unit-of-work + repository abstraction; wrapping it adds indirection, not testability — integration tests cover real persistence behavior). No MediatR: slices are invoked directly from minimal-API-style endpoint registrations, so there is no pipeline indirection to debug; cross-cutting concerns (validation, exception handling) live in explicit middleware/endpoint filters instead of a mediator pipeline.

**Trade-off accepted:** handlers couple to EF Core. Mitigation: the coupling is *visible and contained* per slice, and the integration-test suite (D7) pins behavior against a real database, so the abstraction a repository would provide is not load-bearing.

### D3 — PostgreSQL via Npgsql

**Rationale:** first-class EF Core provider, ~80 MB container that starts in seconds (vs ~1.5 GB for SQL Server), zero licensing friction, and the reviewer can run the whole system with one `docker compose up`. Case-insensitive uniqueness uses PostgreSQL's `citext`-style collation via `ILike`/lower-index — handled with a unique index on `lower(name)`. **Alternatives considered:** SQL Server (strong enterprise fit, poor reviewer experience), MySQL (weakest EF Core provider story via Pomelo).

### D4 — Error contract: ProblemDetails + global middleware + validation pipeline

- FluentValidation validators per slice, run by an endpoint filter before the handler; failures short-circuit to `400` with the `errors` dictionary from the api-contract spec.
- Business-rule conflicts (duplicate name/ISBN, delete-in-use) raise a small `ConflictException`/`NotFoundException` pair mapped to `409`/`404` ProblemDetails by the global middleware.
- Unhandled exceptions → `500` ProblemDetails with a correlation ID (`TraceIdentifier`) that is also in the structured log entry. Stack traces never leave the server.

**Rationale:** one uniform error contract is three differentiator boxes (global error handling, validation pipeline, security awareness) that reinforce each other instead of being three disjoint features.

### D5 — Angular SPA: standalone components + signals, feature-folder layout

```
frontend/src/app/
├── core/       api client (provideHttpClient + interceptors), error-to-message mapping
├── features/
│   ├── genres/   list + form components, genre.service.ts
│   ├── authors/  (same shape)
│   └── books/    list + form (author/genre selectors), book.service.ts
└── app.routes.ts
```

Signals for component state; the HTTP layer returns Observables mapped into signals at the component boundary. Reactive forms for create/edit with server validation errors written back onto controls. A single HTTP interceptor maps ProblemDetails (400 field errors → form controls; 409/404/500 → toast/message banner).

**Rationale:** Angular chosen (over React) for built-in structure that mirrors the backend's discipline and enterprise alignment; standalone+signals is the modern, boilerplate-light Angular idiom. No NgRx — server state is simple and cache requirements are trivial; a store would be the frontend equivalent of the repository pattern.

### D6 — Pagination/filter/sort implemented once, generically

A `PagedQuery` record (page, pageSize, sort, direction) + an EF Core `ApplyPaging` extension used by all list slices. Sort fields are whitelisted per endpoint via a switch expression mapping the public sort name to the key selector — unsupported values are rejected by the query validator, never reaching SQL. Deterministic tie-breaker: always append `Id` as final sort key.

### D7 — Testing strategy: unit + Testcontainers integration

- **Unit tests** (xUnit + FluentAssertions): validators (all field rules incl. ISBN checksum, year bound), business-rule branches (uniqueness, delete protection) using EF Core InMemory *only where EF semantics don't matter*; ISBN/value-object logic as pure functions.
- **Integration tests** (xUnit + WebApplicationFactory + Testcontainers PostgreSQL): each API scenario from the specs — CRUD happy paths, 400/404/409 branches, pagination envelope metadata, sort whitelist rejection, ProblemDetails content-type. Real migrations applied to a real database per test class via `Respawn`-style reset or schema-per-class isolation.

**Rationale:** integration tests against real PostgreSQL are what make D2 defensible — behavior is pinned at the HTTP boundary with real persistence, so internal structure can evolve freely. **Alternative considered:** mocked repositories — rejected; mocks verify their own setup, not the system.

### D8 — Containerization and local run

`docker-compose.yml` with three services: `db` (postgres:16-alpine, healthcheck), `api` (multi-stage Dockerfile, waits on db healthcheck, runs migrations at startup in Development), `web` (Angular build → nginx static serve, proxying `/api` to the api service). One command: `docker compose up --build`.

### D9 — ADRs

`docs/adr/0001-vertical-slices.md`, `0002-postgresql.md`, `0003-no-repository-pattern.md`, `0004-error-contract.md` — each ~15 lines: context, decision, consequences. The README links these plus run instructions; the OpenSpec change remains the deeper design record.

## Data model

```
Genre  (Id uuid PK, Name citext-unique ≤100)
Author (Id uuid PK, Name citext-unique ≤150, Bio ≤2000 null)
Book   (Id uuid PK, Title ≤200, Isbn ≤17 null + unique filtered index,
        PublishedYear int null, AuthorId FK→Author RESTRICT, GenreId FK→Genre RESTRICT,
        index on (AuthorId), (GenreId))
```

FK delete behavior is `Restrict` in the database *and* pre-checked in the delete slices — the DB constraint is the backstop, the 409 is the contract. UUIDv7-style sequential GUIDs keep index inserts friendly.

## Risks / Trade-offs

- Vertical slices read as "unstructured" to a layer-expecting reviewer → README + ADR-0001 frame it as deliberate right-sizing, with the Clean Architecture evolution path named explicitly.
- No auth on an API challenge could look like an omission → proposal + README call it out as scoped future work with the intended design (JWT bearer, policy-based authorization), turning the gap into a discussed decision.
- Testcontainers requires Docker at test time; a reviewer without Docker can't run integration tests → unit tests run anywhere; README documents the requirement and `dotnet test --filter` escape hatch.
- EF Core InMemory used in some unit tests doesn't enforce real constraints → constraint-dependent behavior is covered *only* in integration tests, never asserted against InMemory.
- 3-day budget: frontend is the most elastic scope → Day 3 feature-freeze rule; list/create/edit/delete + error surfacing are the committed core, polish is expendable.

## Open Questions

- Angular version pin (17 vs 18/19 LTS) — resolved at scaffold time by `ng new` defaults; no spec or task impact.
