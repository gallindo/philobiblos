# Proposal: Refactor backend to Clean Architecture with repository pattern

## Why

The current backend is organized as vertical slices that inject `LibraryDbContext` directly into every minimal-API handler. This works for a small CRUD domain, but it couples persistence concerns (EF Core queries, `SaveChangesAsync`, `AsNoTracking`) with HTTP endpoint logic and business rules. Clean Architecture decouples the domain/application from frameworks, making the core logic testable without a database and making future changes to persistence or UI cheaper.

## What Changes

- Introduce a classic Clean Architecture project/layer split:
  - `Domain` — entities, value objects, domain exceptions, repository contracts (`IAuthorRepository`, `IBookRepository`, `IGenreRepository`).
  - `Application` — use-case commands/queries, command/query handlers, DTOs, validation rules that depend only on abstractions.
  - `Infrastructure` — EF Core `LibraryDbContext`, repository implementations, migrations, EF configurations, middleware, logging.
  - `Api` (or `Web`) — minimal-API endpoint registration, HTTP contracts, endpoint filters.
- Replace direct `LibraryDbContext` usage in handlers with repository calls and an explicit unit-of-work boundary.
- Move request/response DTOs and mapping closer to the application or API layer; keep domain entities persistence-ignorant.
- Rewrite the in-memory business-rule tests to exercise application handlers/repositories instead of `DbContext` directly; keep integration tests as the outer safety net.
- Update ADR 0003 and ADR 0001 to record the architectural shift and the rationale for the repository pattern.

**BREAKING**: Internal project structure and namespaces will change. The HTTP contract (routes, status codes, error shape, JSON responses) remains unchanged.

## Capabilities

No spec-level behavior changes. This is a structural refactor; the public API contract and acceptance criteria stay the same.

## Impact

- All `backend/src/Philobiblos.Api/Features/**` handler files will be reorganized into Application/Domain handlers.
- `LibraryDbContext` moves to `Infrastructure` and is no longer referenced by endpoint handlers.
- Unit tests currently calling handlers with an in-memory `LibraryDbContext` must target Application-layer handlers or repository interfaces.
- Integration tests remain largely unchanged because they assert HTTP behavior, but project references will need updating.
- Build pipeline, Docker setup, and `Program.cs` registration code will need minor adjustments for the new assembly/layer structure.
