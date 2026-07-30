# Tasks: bootstrap-library-management

Day mapping (3-day budget): groups 1–4 ≈ Day 1, groups 5–7 ≈ Day 2, groups 8–10 ≈ Day 3 (frontend feature-freeze at Day 3 noon).

## 1. Solution scaffold

- [x] 1.1 Create `backend/Philobiblos.sln` with `src/Philobiblos.Api`, `tests/Philobiblos.UnitTests`, `tests/Philobiblos.IntegrationTests` (.NET 10 LTS, xUnit wired and green)
- [x] 1.2 Add NuGet dependencies: Npgsql.EntityFrameworkCore.PostgreSQL, FluentValidation, Serilog.AspNetCore (API); FluentAssertions, Testcontainers.PostgreSql, Microsoft.AspNetCore.Mvc.Testing (tests)
- [x] 1.3 Create the design.md D1 folder layout (`Features/`, `Domain/`, `Data/`, `Infrastructure/`) and `.editorconfig` + nullable enable
- [ ] 1.4 Scaffold `frontend/` Angular workspace (standalone + signals default), verify `ng build` passes

## 2. Domain and persistence

- [x] 2.1 Implement `Genre`, `Author`, `Book` entities per design.md data model (UUID keys, field constraints)
- [x] 2.2 Implement `LibraryDbContext` with EF configurations: unique indexes on `lower(Name)`, filtered unique index on ISBN, `Restrict` delete behavior, indexes on FKs
- [x] 2.3 Create and verify the initial EF Core migration against a local Docker PostgreSQL 16

## 3. Cross-cutting API infrastructure (api-contract spec)

- [x] 3.1 Global exception middleware: maps NotFound/Conflict exceptions to 404/409 ProblemDetails, unhandled to 500 with correlation ID, no stack-trace leakage
- [x] 3.2 Validation endpoint filter running FluentValidation per slice, short-circuiting to `400` problem details with the `errors` field dictionary
- [x] 3.3 Serilog request logging + enrichers (correlation ID from TraceIdentifier), console sink with structured output
- [x] 3.4 `PagedQuery` record + `ApplyPaging` EF extension: page/pageSize defaults and bounds (1..100), per-endpoint sort whitelist via switch mapping, `Id` tie-breaker, envelope `{ items, page, pageSize, totalCount }`
- [x] 3.5 Swagger/OpenAPI in Development; verify ProblemDetails `application/problem+json` content type on all error paths

## 4. Genre, author, and book slices

- [x] 4.1 Genre slices (create/list/get/update/delete) with validators, name-uniqueness conflict, delete-in-use protection — all genre-management spec scenarios passing via HTTP
- [x] 4.2 Author slices with same shape plus optional `bio` field — author-management spec scenarios passing
- [x] 4.3 Book slices with author/genre existence validation, ISBN-10/13 checksum validation + uniqueness, publication-year bound, combined filters (title + authorId + genreId), response DTOs embedding author and genre id+name — book-management spec scenarios passing

## 5. Unit tests

- [ ] 5.1 Validator tests for every field rule (name lengths, title required, ISBN format/checksum, year 1450..current, pagination bounds, sort whitelist)
- [ ] 5.2 Business-rule branch tests: duplicate-name detection, delete-in-use detection (EF semantics-free only, per design.md D7)

## 6. Integration tests (Testcontainers)

- [ ] 6.1 Test fixture: Testcontainers PostgreSQL + WebApplicationFactory, migrations applied, per-class data isolation
- [ ] 6.2 CRUD happy-path scenarios for all three entities against the real database
- [ ] 6.3 Error-contract scenarios: 400 shape with `errors`, 404, 409 (duplicate name, duplicate ISBN, delete-in-use), pagination metadata and out-of-range rejection, sort whitelist rejection

## 7. Containerization

- [ ] 7.1 Multi-stage API Dockerfile (sdk build → aspnet runtime), migrations applied at startup in Development
- [ ] 7.2 Frontend Dockerfile (node build → nginx) with `/api` reverse proxy to the api service
- [ ] 7.3 `docker-compose.yml`: db (healthcheck) → api → web dependency chain; verify cold `docker compose up --build` yields a working system

## 8. Angular SPA (library-spa spec)

- [ ] 8.1 Core: typed API client services, HTTP interceptor mapping ProblemDetails (400→form controls, 409/404/500→banner), app shell with section navigation
- [ ] 8.2 Genres feature: list (search + pagination), create/edit reactive form with server-error writeback, delete with confirmation
- [ ] 8.3 Authors feature: same shape as genres
- [ ] 8.4 Books feature: list showing author/genre names, form with catalog-driven author/genre selectors, delete with confirmation
- [ ] 8.5 Loading indicators, empty states, and in-use delete message per library-spa spec; final pass against every library-spa scenario

## 9. Documentation

- [ ] 9.1 ADRs 0001–0004 in `docs/adr/` per design.md D9
- [ ] 9.2 README: solution overview, architecture, backend/frontend organization, database choice, trade-offs, testing strategy, known limitations, with-more-time improvements, and one-command run instructions
- [ ] 9.3 Verify README instructions end-to-end from a clean clone (fresh terminal, `docker compose up --build`, run test suites)

## 10. Final verification

- [ ] 10.1 `dotnet test` green (unit + integration), `ng build` green, `openspec validate bootstrap-library-management --strict` clean
- [ ] 10.2 Walk every spec scenario (genre-management, author-management, book-management, api-contract, library-spa) and confirm observable pass
