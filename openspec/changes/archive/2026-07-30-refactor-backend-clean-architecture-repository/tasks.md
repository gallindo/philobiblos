## 1. Create Clean Architecture projects

- [x] 1.1 Add `Philobiblos.Domain` class library and move domain entities / `IEntity` from `Philobiblos.Api`.
- [x] 1.2 Add `Philobiblos.Application` class library and move DTOs/mapping/validators from `Features/`.
- [x] 1.3 Add `Philobiblos.Infrastructure` class library and move `Data/`, `LibraryDbContext`, EF configurations, migrations, exception middleware, and persistence-related infrastructure from `Philobiblos.Api`.
- [x] 1.4 Convert `Philobiblos.Api` into a thin web project that references `Application` and `Infrastructure`.
- [x] 1.5 Update solution file and project references to enforce the dependency rule.

## 2. Define repository contracts

- [x] 2.1 Add `IRepository<T>` base interface with `GetByIdAsync`, `Add`, `Remove`, `AnyAsync`, `ListAsync`, `CountAsync`.
- [x] 2.2 Add `IAuthorRepository` with `IsNameTakenAsync` and `IsAuthorInUseAsync`.
- [x] 2.3 Add `IGenreRepository` with `IsNameTakenAsync` and `IsGenreInUseAsync`.
- [x] 2.4 Add `IBookRepository` with `IsIsbnTakenAsync`, `GetByIdWithDetailsAsync`, and list/filter/sort methods.
- [x] 2.5 Add `IUnitOfWork` with `SaveChangesAsync` and wire it through the same EF context lifetime.

## 3. Implement infrastructure repositories

- [x] 3.1 Implement concrete repositories in `Philobiblos.Infrastructure` using `LibraryDbContext`.
- [x] 3.2 Move persistence logic (`Include`, `AsNoTracking`, `ProjectToResponse`) into repository implementations.
- [x] 3.3 Register repositories and `IUnitOfWork` with DI in `Philobiblos.Api`.

## 4. Refactor application layer

- [x] 4.1 Convert each `Features/<Entity>` static handler into an Application command/query and handler class.
- [x] 4.2 Move business-rule checks (uniqueness, in-use, reference validation) into Application handlers.
- [x] 4.3 Keep input validation (length, format, pagination) in FluentValidation validators.
- [x] 4.4 Move request/response DTOs and mapping to `Application`.

## 5. Refactor API / presentation layer

- [x] 5.1 Update minimal-API endpoints to construct Application commands/queries and invoke handlers.
- [x] 5.2 Map Application DTOs to HTTP results (`CreatedAtRoute`, `Ok`, `NoContent`).
- [x] 5.3 Keep `ProblemDetails` exception middleware and validation filter behavior unchanged.

## 6. Update tests

- [x] 6.1 Rewrite unit tests / business-rule tests to use fakes or in-memory repository implementations.
- [x] 6.2 Keep integration tests as the HTTP contract safety net; update project references only.
- [x] 6.3 Run the full backend test suite and fix any broken references.

## 7. Documentation and validation

- [x] 7.1 Update ADR 0001 and ADR 0003 to record the move to Clean Architecture and repository pattern.
- [x] 7.2 Update README architecture section and diagram.
- [x] 7.3 Run `dotnet test` and Docker Compose e2e tests to verify no regressions.
- [x] 7.4 Run `openspec validate` and archive the change.
