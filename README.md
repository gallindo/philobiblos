# Philobiblos

A small but production-aware library management system for a Senior Software Engineer technical challenge. It manages **genres**, **authors**, and **books** with case-insensitive uniqueness, delete protection, ISBN validation, and a uniform HTTP error contract.

## Solution overview

- **Backend:** .NET 10 minimal-API application (`backend/src/Philobiblos.Api/`)
- **Frontend:** Angular 19 standalone SPA (`frontend/`)
- **Database:** PostgreSQL 16
- **Observability:** Serilog structured logging with correlation IDs
- **Run orchestration:** Docker Compose (`docker-compose.yml`)

The repository is intentionally small (three entities, CRUD use cases) so the focus is on justified architecture, a coherent API contract, and a clean local run experience.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22](https://nodejs.org/)
- [Docker](https://www.docker.com/) & Docker Compose
- Angular CLI (optional): `npm install -g @angular/cli`

## Quick start

### One-command full stack

```bash
docker compose up --build
```

Services come up as:

| Service | URL / Port | Notes |
|---|---|---|
| PostgreSQL | `localhost:5432` | Database `philobiblos` |
| API | `http://localhost:8080` | Migrations auto-apply in Development |
| Web | `http://localhost:4200` | Angular SPA served by nginx |

Tear down and remove the volume:

```bash
docker compose down -v
```

### Backend only

```bash
cd backend
dotnet run --project src/Philobiblos.Api/Philobiblos.Api.csproj
```

Requires a PostgreSQL instance on `localhost:5432` matching `appsettings.json` (`Database=philobiblos;Username=postgres;Password=postgres`). Migrations auto-apply in Development.

### Frontend only

```bash
cd frontend
npm install
npm run start
```

`ng serve` runs on `http://localhost:4200` and proxies `/api` requests to `http://localhost:8080` via `proxy.conf.json`.

## Architecture

The backend uses **Vertical Slice Architecture**: each feature is a self-contained file (route, DTOs, validator, handler) rather than a layered controller→service→repository stack. Cross-cutting concerns live in `Infrastructure/`.

The frontend uses **feature folders** plus a thin `core/` layer for HTTP and error handling. Component state uses Angular signals; HTTP streams are mapped into signals at the component boundary.

## Backend organization

```
backend/src/Philobiblos.Api/
├── Features/
│   ├── Genres/       CreateGenre.cs, ListGenres.cs, GetGenre.cs, UpdateGenre.cs, DeleteGenre.cs
│   ├── Authors/      (same shape)
│   └── Books/        (same shape)
├── Domain/           Genre.cs, Author.cs, Book.cs, IEntity.cs
├── Data/             LibraryDbContext.cs, configurations, migrations
└── Infrastructure/   ExceptionHandlingMiddleware.cs, ValidationFilter.cs, Paging.cs, exceptions
```

- **One file per use case.** A slice owns everything that changes together.
- **No MediatR.** Slices are invoked directly from minimal-API endpoint registrations.
- **No repository pattern.** Handlers use `LibraryDbContext` directly; the coupling is visible per slice and covered by integration tests.

## Frontend organization

```
frontend/src/app/
├── core/
│   ├── api.service.ts                Typed HTTP client for all entities
│   ├── problem-details.interceptor.ts Maps 400 field errors to forms, 409/404/500 to messages
│   ├── error.service.ts              Reactive error banner state
│   └── models.ts                     Shared DTOs and `PagedResult<T>`
├── features/
│   ├── genres/         genre-list.component
│   ├── authors/        author-list.component
│   └── books/          book-list.component
├── shared/
│   └── components/
│       └── pagination.component
└── app.routes.ts
```

- Reactive forms for create/edit with server validation errors written back onto controls.
- A single HTTP interceptor normalizes every backend error into one `ApiError` shape.
- No NgRx store; component-level signals are sufficient for this scope.

## Database choice

PostgreSQL 16 is used via the Npgsql EF Core provider.

- Case-insensitive uniqueness for genre/author names via function-based indexes on `lower("Name")`.
- Optional ISBN uniqueness via a filtered unique index on `Books.Isbn` where `Isbn IS NOT NULL`.
- Foreign keys from `Books` to `Authors` and `Genres` use `Restrict` delete behavior; the application pre-checks references and returns `409 Conflict` when deletion would leave orphans.

## Main trade-offs

Key decisions are captured in ADRs under `docs/adr/`:

1. **Vertical slices over Clean Architecture layers** — less ceremony for a small CRUD domain; named evolution path if aggregates/domain events appear.
2. **PostgreSQL over SQL Server/MySQL** — fast container startup, first-class Npgsql provider, no licensing.
3. **No repository pattern / no MediatR** — EF Core directly in handlers, explicit middleware for cross-cutting concerns.
4. **ProblemDetails + global middleware + FluentValidation filter** — uniform error contract, no stack-trace leakage, correlation IDs.
5. **Authentication deferred** — JWT auth and RBAC are documented future work; a half-baked implementation under time pressure would be a liability.

## Testing strategy

- **Unit tests** (`backend/tests/Philobiblos.UnitTests/`): validator rules, ISBN checksum logic, pagination bounds, and business-rule branches that do not depend on real persistence.
- **Integration tests** (`backend/tests/Philobiblos.IntegrationTests/`): `WebApplicationFactory` + Testcontainers PostgreSQL. Each scenario hits the real HTTP boundary with real migrations and persistence, including 400/404/409 shapes, pagination metadata, and sort whitelist rejection.

Run all tests:

```bash
cd backend
dotnet test
```

> Docker is required for integration tests. To run only unit tests: `dotnet test --filter FullyQualifiedName~UnitTests`

## Known limitations

- No authentication or authorization.
- No audit logging or audit columns.
- No soft deletes; records are physically removed.
- No OpenTelemetry exporters, CI pipeline, or deployment target beyond Docker Compose.
- No client-side e2e tests.

## Improvements with more time

- JWT bearer authentication with policy-based RBAC.
- Audit columns (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`).
- Soft deletes and a recycle-bin workflow.
- CI pipeline (build, test, container image publish).
- Richer Angular filtering/sorting (multi-column, debounced search).
- A React alternative version of the SPA for comparison, or Storybook for component documentation.
