# Backend Clean Architecture / Repository Pattern Refactoring Report

## Executive Summary

The current Philobiblos backend is a single .NET 10 minimal-API project organized as **vertical slices**. Each feature file (`CreateAuthor.cs`, `UpdateBook.cs`, etc.) contains route registration, DTOs, validators, and the handler that talks directly to `LibraryDbContext`. This keeps the codebase small and easy to change for a three-entity CRUD domain, but it couples persistence, business rules, and HTTP presentation in one place.

This report identifies the concrete refactoring needed to move the backend to a **Clean Architecture** with the **Repository pattern**. The public HTTP contract (routes, status codes, `ProblemDetails` errors) should remain unchanged. The refactor is a non-behavioral change aimed at improving testability, separation of concerns, and long-term maintainability.

## Current Architecture Snapshot

```
backend/src/Philobiblos.Api/
├── Domain/
│   ├── Author.cs, Book.cs, Genre.cs, IEntity.cs
├── Data/
│   ├── LibraryDbContext.cs, AuthorConfiguration.cs, BookConfiguration.cs, GenreConfiguration.cs
│   └── Migrations/
├── Features/
│   ├── Authors/   CreateAuthor.cs, UpdateAuthor.cs, DeleteAuthor.cs, GetAuthor.cs, ListAuthors.cs, AuthorDtos.cs
│   ├── Books/     CreateBook.cs, UpdateBook.cs, DeleteBook.cs, GetBook.cs, ListBooks.cs, BookDtos.cs, IsbnValidator.cs
│   └── Genres/    CreateGenre.cs, UpdateGenre.cs, DeleteGenre.cs, GetGenre.cs, ListGenres.cs, GenreDtos.cs
├── Infrastructure/
│   ├── Paging.cs, Exceptions.cs, ExceptionHandlingMiddleware.cs, ValidationFilter.cs
└── Program.cs
```

`Program.cs` registers `LibraryDbContext` and maps each endpoint through extension methods in the slice files.

## Where the Current Design Couples Concerns

### 1. Handlers depend directly on EF Core

Every handler receives `LibraryDbContext` and uses `AnyAsync`, `FirstOrDefaultAsync`, `Include`, `AsNoTracking`, `SaveChangesAsync`, and `ProjectToResponse` extensions. Example from `CreateBook.cs`:

```csharp
internal static async Task<Results<CreatedAtRoute<BookResponse>, ValidationProblem>> Handle(
    CreateBookRequest request,
    LibraryDbContext db,
    CancellationToken cancellationToken)
```

This means the application logic cannot be tested without an EF Core provider (currently in-memory in unit tests, real PostgreSQL in integration tests).

### 2. Business rules are mixed with persistence queries

Uniqueness checks, reference validation, and in-use checks are written inline in the handlers:

- `CreateAuthor` / `UpdateAuthor` query `db.Authors.AnyAsync(...)` for name uniqueness.
- `CreateBook` / `UpdateBook` query `db.Authors` and `db.Genres` to validate references.
- `DeleteAuthor` / `DeleteGenre` query `db.Books.AnyAsync(...)` before deletion.
- ISBN uniqueness is checked directly against `db.Books`.

These rules are hard to unit test in isolation because they require the database context.

### 3. DTOs and mapping live inside feature folders

`AuthorDtos.cs`, `BookDtos.cs`, and `GenreDtos.cs` are in the API layer. With Clean Architecture, response shapes should be owned by the application layer so the API layer can stay focused on HTTP.

### 4. Domain entities are persistence-ignorant but underused

`Author`, `Book`, and `Genre` are simple data bags with no behavior. The current design does not take advantage of encapsulation; business rules live in handlers instead of the domain model.

### 5. Unit tests rely on the in-memory EF Core provider

`AuthorBusinessRuleTests` and `GenreBusinessRuleTests` spin up an in-memory `LibraryDbContext` and call the static handlers directly. This tests persistence and business rules together, blurring the boundary between the two.

### 6. The project has no explicit application layer

The current layers are `Domain`, `Data`, `Infrastructure`, and `Features`. The last one is effectively a mix of application logic and presentation wiring.

## Target Architecture

```
Philobiblos.Api/        (Presentation layer — minimal endpoints, DI wiring, middleware)
├── Reference: Application + Infrastructure

Philobiblos.Application/ (Application layer — commands, queries, handlers, DTOs, validators)
├── Reference: Domain

Philobiblos.Domain/     (Domain layer — entities, value objects, repository interfaces)
├── No external references

Philobiblos.Infrastructure/ (Infrastructure layer — EF Core, repositories, migrations, middleware)
├── Reference: Application + Domain
```

### Dependency Rule

- `Domain` knows nothing about frameworks.
- `Application` knows `Domain` and abstractions (`IRepository<T>`, `IUnitOfWork`).
- `Infrastructure` implements the abstractions.
- `Api` knows `Application` and `Infrastructure` only for DI wiring.

## Repository Contracts

### Common contract

```csharp
public interface IRepository<T> where T : IEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    IReadOnlyList<T> ListAsync(...); // or paged list
    void Add(T entity);
    void Remove(T entity);
}
```

### Aggregate-specific contracts

```csharp
public interface IAuthorRepository : IRepository<Author>
{
    Task<bool> IsNameTakenAsync(string name, Guid? excludingId = null, CancellationToken ct = default);
    Task<bool> IsAuthorInUseAsync(Guid id, CancellationToken ct = default);
}

public interface IGenreRepository : IRepository<Genre>
{
    Task<bool> IsNameTakenAsync(string name, Guid? excludingId = null, CancellationToken ct = default);
    Task<bool> IsGenreInUseAsync(Guid id, CancellationToken ct = default);
}

public interface IBookRepository : IRepository<Book>
{
    Task<bool> IsIsbnTakenAsync(string isbn, Guid? excludingId = null, CancellationToken ct = default);
    Task<Book?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<PagedList<Book>> ListBooksAsync(...);
}
```

### Unit of work

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

`IUnitOfWork` is implemented by the same `LibraryDbContext` wrapper so that all repository operations in a single request share the same transaction context.

## Refactoring Steps

1. **Project split**
   - Create `Philobiblos.Domain`, `Philobiblos.Application`, and `Philobiblos.Infrastructure`.
   - Move entities and repository interfaces to `Domain`.
   - Move `LibraryDbContext`, EF configurations, migrations, and repository implementations to `Infrastructure`.
   - Convert `Philobiblos.Api` into a thin web host.

2. **Repository implementation**
   - Implement the three repositories using EF Core internally.
   - Keep persistence details (`Include`, `AsNoTracking`, `ProjectToResponse`) inside the repositories.

3. **Application handlers**
   - Replace the static `CreateAuthor.Handle(...)` methods with `CreateAuthorCommand` / `CreateAuthorHandler` classes.
   - Move business rules into handlers.
   - Keep input validation (length, format, pagination) in FluentValidation validators.

4. **API layer**
   - Update minimal-API endpoints to build commands/queries and call handlers.
   - Map Application DTOs to HTTP results.
   - Keep `ProblemDetails` and validation middleware untouched.

5. **Tests**
   - Convert unit/business-rule tests to use fakes or an in-memory `IUnitOfWork` + repository implementation.
   - Keep integration tests as the HTTP contract safety net.

6. **Documentation**
   - Update ADR 0001 and ADR 0003 to reflect the architectural shift.
   - Update the README architecture section.

## Impact on Tests

| Test type | Current approach | New approach |
|---|---|---|
| Unit tests | Validate validator rules | Keep; validators stay in Application |
| Business-rule tests | In-memory `LibraryDbContext` + handler | Fake repositories / in-memory UoW + handler |
| Integration tests | HTTP boundary + real PostgreSQL | Unchanged; still validates the contract |

## Risks and Trade-offs

- **More projects/files for a small domain.** The refactor adds ceremony. The benefit only pays off if the domain grows or if the team values isolated unit tests.
- **Shallow repository methods.** EF Core is already an abstraction. The repository layer should be kept thin and aggregate-specific; avoid generic `IQueryable` leakage.
- **Test rewrite effort.** The in-memory business-rule tests need to change, but a small in-memory repository set can minimize the churn.
- **Public API must remain unchanged.** The existing integration tests and e2e tests are the contract guardrail.

## Recommendations

1. **Proceed with the refactor if** the goal is to demonstrate Clean Architecture for a technical review, or if the project is expected to grow beyond pure CRUD.
2. **Keep the refactor minimal.** Do not introduce MediatR, CQRS, domain events, or value objects unless a new requirement demands them.
3. **Do not change the HTTP contract.** Treat the existing integration tests as the acceptance criteria.
4. **Start with the project split and repository interfaces**, then move one feature at a time (Author → Genre → Book) to keep the build green.
5. **Consider keeping the current vertical-slice approach** if the project will remain small and the priority is delivery speed over architectural purity. The current code is well-structured for its scope.

## Conclusion

The refactoring is feasible and well-defined. The main work is mechanical project reorganization, moving persistence logic behind repositories, and rewriting the unit-test layer to use those abstractions. The public API, the Angular client, and the Docker Compose setup can remain unchanged. The decision to refactor should be based on whether the project will grow beyond its current CRUD scope, because Clean Architecture adds meaningful value mainly when the domain and testing surface expand.
