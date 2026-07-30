# Proposal: bootstrap-library-management

## Why

Philobiblos is a greenfield repo created for a Senior Software Engineer technical challenge: deliver, in 3 days, a working library management system (genres, authors, books) that demonstrates coherent architecture, justified technical decisions, and tech-lead-grade reasoning. Nothing exists yet — this change bootstraps the entire solution: backend API, frontend SPA, database, tests, containerization, and decision documentation.

## What Changes

- **Backend**: .NET/C# REST API using Vertical Slice Architecture — one self-contained slice per use case, EF Core used directly inside slices (no repository layer, no mediator library; both omissions are deliberate, documented trade-offs).
- **Database**: PostgreSQL via Npgsql EF Core provider, code-first migrations, runs in Docker.
- **Domain**: `Genre`, `Author`, `Book` entities. A book belongs to exactly one author and one genre; authors and genres have many books.
- **Business rules beyond raw CRUD** (permitted and encouraged by the challenge):
  - Genre and author names are unique (case-insensitive) → `409 Conflict`.
  - Authors/genres with associated books cannot be deleted → `409 Conflict`.
  - Book ISBN is optional but format-validated and unique when present; publication year must not be in the future → `422`.
- **API contract**: consistent responses — RFC 7807 `ProblemDetails` for all errors, a global exception middleware (no stack-trace leakage), a pagination envelope on list endpoints with filter/sort support, and a FluentValidation pipeline producing structured validation errors.
- **Observability**: Serilog structured logging (request logging + contextual enrichers).
- **Frontend**: Angular SPA (standalone components, signals) to list, register, edit, and remove genres, authors, and books, surfacing book→author/genre relationships.
- **Tests**: xUnit unit tests for business rules/validators; integration tests with `WebApplicationFactory` + Testcontainers running real PostgreSQL for the main API scenarios.
- **Containerization**: `docker-compose.yml` orchestrating database, API, and frontend for one-command local execution.
- **Documentation**: README (run instructions + decision rationale) and short ADRs under `docs/adr/` for the significant decisions.
- **Explicitly out of scope** (documented as future work, not forgotten): authentication/authorization, full OpenTelemetry observability, CI pipeline.

## Capabilities

### New Capabilities

- `genre-management`: CRUD and search for genres; name uniqueness; delete protection when books reference the genre.
- `author-management`: CRUD and search for authors; name uniqueness; delete protection when books reference the author.
- `book-management`: CRUD and search for books; required single-author and single-genre references; ISBN/publication-year validation; paginated, filterable, sortable listing.
- `api-contract`: cross-cutting HTTP behavior — ProblemDetails error contract, global exception handling, validation error shape, pagination envelope, consistent status codes.
- `library-spa`: the Angular single-page application exposing all entity management flows and the book→author/genre relationship to users.

### Modified Capabilities

(none — greenfield)

## Impact

- **New code**: entire repository contents — `backend/` (.NET solution: API + unit & integration test projects), `frontend/` (Angular workspace), `docker-compose.yml`, `docs/adr/`, README.
- **APIs**: REST endpoints under `/api/genres`, `/api/authors`, `/api/books`.
- **Dependencies**: .NET 8 SDK, EF Core 8 + Npgsql, FluentValidation, Serilog, xUnit, Testcontainers, Angular 17+, Node LTS, Docker.
- **Systems**: PostgreSQL 16 (containerized); no external services.
