- [1. Philobiblos](#1-philobiblos)
  - [1.1. Solution overview](#11-solution-overview)
  - [1.2. Prerequisites](#12-prerequisites)
  - [1.3. Quick start](#13-quick-start)
    - [1.3.1. One-command full stack](#131-one-command-full-stack)
    - [1.3.2. Observability](#132-observability)
    - [1.3.3. Backend only](#133-backend-only)
    - [1.3.4. Frontend only](#134-frontend-only)
  - [1.4. Architecture](#14-architecture)
  - [1.5. Diagram](#15-diagram)
  - [1.6. Dependency rule](#16-dependency-rule)
  - [1.7. Backend organization](#17-backend-organization)
  - [1.8. Frontend organization](#18-frontend-organization)
  - [1.9. Database choice](#19-database-choice)
  - [1.10. Main trade-offs](#110-main-trade-offs)
  - [1.11. Testing strategy](#111-testing-strategy)
  - [1.12. Authentication](#112-authentication)
    - [1.12.1. Google OAuth](#1121-google-oauth)
    - [1.12.2. Email / password](#1122-email--password)
    - [1.12.3. Default administrator account](#1123-default-administrator-account)
    - [1.12.4. Local development and e2e tests without credentials](#1124-local-development-and-e2e-tests-without-credentials)
  - [1.13. Known limitations](#113-known-limitations)
  - [1.14. Improvements with more time](#114-improvements-with-more-time)

---

# 1. Philobiblos

A small but production-aware library management system for a Senior Software Engineer technical challenge. It manages **genres**, **authors**, and **books** with case-insensitive uniqueness, delete protection, ISBN validation, and a uniform HTTP error contract.

## 1.1. Solution overview

- **Backend:** .NET 10 minimal-API application (`backend/src/Philobiblos.Api/`)
- **Frontend:** Angular 19 standalone SPA (`frontend/`)
- **Database:** PostgreSQL 16
- **Observability:** Serilog structured logging with correlation IDs, OpenTelemetry traces, Prometheus metrics, and health checks
- **Run orchestration:** Docker Compose (`docker-compose.yml`)

The repository is intentionally small (three entities, CRUD use cases) so the focus is on justified architecture, a coherent API contract, and a clean local run experience.

## 1.2. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22](https://nodejs.org/)
- [Docker](https://www.docker.com/) & Docker Compose
- Angular CLI (optional): `npm install -g @angular/cli`

## 1.3. Quick start

### 1.3.1. One-command full stack

```bash
docker compose up --build
```

Services come up as:

| Service | URL / Port | Notes |
|---|---|---|
| PostgreSQL | `localhost:5432` | Database `philobiblos` |
| API | `http://localhost:8080` | Migrations auto-apply in Development |
| Web | `http://localhost:4200` | Angular SPA served by nginx |
| OpenTelemetry Collector | `localhost:4317` (gRPC), `localhost:4318` (HTTP) | Receives OTLP traces and metrics from the API |
| Prometheus | `http://localhost:9090` | Scrapes metrics from the collector |
| Jaeger | `http://localhost:16686` | Trace search and visualization UI |

Tear down and remove the volume:

```bash
docker compose down -v
```

### 1.3.2. Observability

The API exposes two anonymous, production-friendly endpoints:

- `GET /health` — database health check (no authentication required).
- `GET /metrics` — Prometheus exposition format with HTTP and runtime metrics.

When running via Docker Compose, the API sends traces and metrics via OTLP to the OpenTelemetry Collector:

- `OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317`
- Traces are forwarded to **Jaeger** (`http://localhost:16686`).
- Metrics are re-exposed by the collector and scraped by **Prometheus** (`http://localhost:9090`).

For `dotnet run` without Docker, the app still serves `/health` and `/metrics`, but no collector, Prometheus, or Jaeger services are started.

### 1.3.3. Backend only

```bash
cd backend
dotnet run --project src/Philobiblos.Api/Philobiblos.Api.csproj
```

Requires a PostgreSQL instance on `localhost:5432` matching `appsettings.json` (`Database=philobiblos;Username=postgres;Password=postgres`). Migrations auto-apply in Development.

### 1.3.4. Frontend only

```bash
cd frontend
npm install
npm run start
```

`ng serve` runs on `http://localhost:4200` and proxies `/api` requests to `http://localhost:8080` via `proxy.conf.json`.

## 1.4. Architecture

The backend uses **Clean Architecture** with an explicit repository pattern:

- **`Domain`** — entities, domain exceptions, repository interfaces, and shared paging primitives. No framework dependencies.
- **`Application`** — commands, queries, handlers, DTOs, validators, result types, and handler abstractions. Depends only on `Domain`.
- **`Infrastructure`** — EF Core, PostgreSQL migrations, repository implementations, exception middleware, and validation filter. Depends on `Domain` and `Application`.
- **`Api`** — thin minimal-API host that maps endpoints, registers DI, and delegates to application handlers.

The frontend uses **feature folders** plus a thin `core/` layer for HTTP and error handling. Component state uses Angular signals; HTTP streams are mapped into signals at the component boundary.

## 1.5. Diagram

```mermaid
flowchart TB
    subgraph Client
        Browser["Browser / Angular SPA"]
    end

    Browser -->|HTTP /api/*| Nginx["nginx (static + proxy)"]

        subgraph Backend
        direction TB
        Api["Philobiblos.Api<br/>Minimal API host<br/>Endpoints + DI + middleware pipeline"]

        App["Philobiblos.Application<br/>Commands, Queries, Handlers<br/>DTOs, Validators, Result&lt;T&gt;<br/>ICommandHandler / IQueryHandler"]

        Infra["Philobiblos.Infrastructure<br/>EF Core + PostgreSQL migrations<br/>Repository implementations<br/>ExceptionHandlingMiddleware + ValidationFilter<br/>Cookie + Google OAuth auth"]

        Domain["Philobiblos.Domain<br/>Entities: Author, Book, Genre, User<br/>Exceptions: NotFoundException, ConflictException<br/>IRepository&lt;T&gt;, IUnitOfWork, PagedList&lt;T&gt;"]
    end

    Nginx --> Api
    Api -->|invokes handlers| App
    Api -->|registers services| Infra
    App -->|depends only on| Domain
    Infra -->|implements interfaces from| Domain
    Infra -->|uses| App
    Infra -->|Npgsql EF Core| Postgres[(PostgreSQL 16)]

    style Domain fill:#d5e8d4,stroke:#82b366,stroke-width:2px
    style App fill:#fff2cc,stroke:#d6b656,stroke-width:2px
    style Infra fill:#dae8fc,stroke:#6c8ebf,stroke-width:2px
    style Api fill:#f8cecc,stroke:#b85450,stroke-width:2px
    style Postgres fill:#e1d5e7,stroke:#9673a6,stroke-width:2px
```

## 1.6. Dependency rule

Dependencies point inward:

- `Philobiblos.Domain` has no project dependencies.
- `Philobiblos.Application` depends only on `Philobiblos.Domain`.
- `Philobiblos.Infrastructure` depends on `Philobiblos.Domain` and `Philobiblos.Application`.
- `Philobiblos.Api` depends on `Philobiblos.Application` and `Philobiblos.Infrastructure` (for DI registration only).



## 1.7. Backend organization

```
backend/src/
├── Philobiblos.Domain/
│   ├── Entities/          Author.cs, Book.cs, Genre.cs, User.cs
│   ├── Exceptions/        NotFoundException.cs, ConflictException.cs
│   ├── Repositories/      IRepository.cs (with IAuthorRepository, IBookRepository, IGenreRepository, IUserRepository, IUnitOfWork)
│   ├── Security/          ICurrentUser.cs
│   └── Common/            PagedList.cs
├── Philobiblos.Application/
│   ├── Authors/           Commands, Queries, DTOs (validators live inside command files)
│   ├── Books/
│   ├── Genres/
│   └── Common/            Result.cs, Unit.cs, PagedQuery.cs, IHandler.cs (ICommandHandler / IQueryHandler)
├── Philobiblos.Infrastructure/
│   ├── Data/              LibraryDbContext.cs, configurations, migrations
│   ├── Repositories/      Repository.cs, AuthorRepository.cs, BookRepository.cs, GenreRepository.cs, UserRepository.cs
│   ├── Security/          AuthOptions.cs, HttpContextCurrentUser.cs
│   ├── Observability/     OpenTelemetry + health-check registration
│   ├── Middleware/        ExceptionHandlingMiddleware.cs
│   ├── Filters/           ValidationFilter.cs
│   └── Paging/            PagingExtensions.cs
└── Philobiblos.Api/
    ├── Endpoints/         AuthEndpoints.cs, AuthorEndpoints.cs, BookEndpoints.cs, GenreEndpoints.cs
    └── Program.cs         DI registration and middleware pipeline
```

- **No MediatR.** Handlers are invoked directly through a simple `ICommandHandler<TCommand,TResult>` / `IQueryHandler<TQuery,TResult>` abstraction.
- **Repository pattern.** EF Core and raw SQL are isolated in the infrastructure layer; the application layer depends on interfaces defined in the domain.
- **Preserved HTTP contract.** Routes, status codes, and ProblemDetails shapes remain unchanged from the previous vertical-slice implementation.

## 1.8. Frontend organization

```
frontend/src/app/
├── core/
│   ├── api.service.ts                Typed HTTP client for all entities
│   ├── auth.service.ts               Current user signal, login/logout helpers
│   ├── auth.interceptor.ts           Redirects to login on 401, shows banner on 403
│   ├── problem-details.interceptor.ts Maps 400 field errors to forms, 409/404/500 to messages
│   ├── error.service.ts              Reactive error banner state
│   └── models.ts                     Shared DTOs, `PagedResult<T>`, and `User`
├── features/
│   ├── login/          login.component
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

## 1.9. Database choice

PostgreSQL 16 is used via the Npgsql EF Core provider.

- Case-insensitive uniqueness for genre/author names via function-based indexes on `lower("Name")`.
- Optional ISBN uniqueness via a filtered unique index on `Books.Isbn` where `Isbn IS NOT NULL`.
- Foreign keys from `Books` to `Authors` and `Genres` use `Restrict` delete behavior; the application pre-checks references and returns `409 Conflict` when deletion would leave orphans.

## 1.10. Main trade-offs

Key decisions are captured in ADRs under `docs/adr/`:

1. **Clean Architecture with repository pattern** — explicit layers isolate domain logic from EF Core, making the code easier to unit-test and evolve as the domain grows. See [ADR 0005](docs/adr/0005-clean-architecture-repository-pattern.md).
2. **PostgreSQL over SQL Server/MySQL** — fast container startup, first-class Npgsql provider, no licensing. See [ADR 0002](docs/adr/0002-postgresql.md).
3. **No MediatR / simple handler abstractions** — `ICommandHandler` and `IQueryHandler` provide just enough indirection without the ceremony of a full message bus. Covered by [ADR 0005](docs/adr/0005-clean-architecture-repository-pattern.md).
4. **ProblemDetails + global middleware + FluentValidation filter** — uniform error contract, no stack-trace leakage, correlation IDs. See [ADR 0004](docs/adr/0004-error-contract.md).
5. **OAuth 2.0 with Google plus cookie sessions** — delegates credential management to Google, avoids storing passwords, and uses policy-based RBAC. See [ADR 0006](docs/adr/0006-oauth-authentication-with-google.md).
6. **OpenTelemetry observability** — traces, runtime metrics, and health checks are collected through the OpenTelemetry SDK and exported via OTLP to a local collector that feeds Jaeger and Prometheus. See [ADR 0007](docs/adr/0007-opentelemetry-observability.md).
7. **Local email/password authentication** — local accounts with PBKDF2 password hashes coexist with Google OAuth and share the same cookie session and role-based authorization. See [ADR 0008](docs/adr/0008-local-email-password-authentication.md).

## 1.11. Testing strategy

- **Unit tests** (`backend/tests/Philobiblos.UnitTests/`): validator rules, ISBN checksum logic, pagination bounds, and business-rule branches that do not depend on real persistence.
- **Integration tests** (`backend/tests/Philobiblos.IntegrationTests/`): `WebApplicationFactory` + Testcontainers PostgreSQL. Each scenario hits the real HTTP boundary with real migrations and persistence, including 400/404/409 shapes, pagination metadata, and sort whitelist rejection.
- **End-to-end tests** (`frontend/e2e/`): Playwright tests against the full Docker Compose stack. They verify the Angular SPA: navigation, loading/empty states, search and pagination, create/edit/delete forms, server error mapping, book relationship visibility, and API error handling.

Run all backend tests:

```bash
cd backend
dotnet test
```

> Docker is required for integration tests. To run only unit tests: `dotnet test --filter FullyQualifiedName~UnitTests`

Run the full e2e suite (builds and starts the stack, runs Playwright, tears down):

```bash
cd frontend
npm run test:e2e:ci
```

To run e2e tests against an already-running stack:

```bash
cd frontend
npm run test:e2e
```

## 1.12. Authentication

The backend supports two authentication paths:

- **Google OAuth 2.0** — `GET /api/auth/login` initiates the Google flow when `Auth:Google:Enabled` is `true`.
- **Email / password** — `POST /api/auth/register` and `POST /api/auth/login` for local accounts.

Both paths issue the same HTTP-only cookie session and participate in the existing `Editor`/`Admin` authorization policies.

### 1.12.1. Google OAuth

`Auth:Google:Enabled` is the switch. Set it to `true` and provide credentials to enable the Google flow, or keep it `false` to rely on local accounts.

```json
{
  "Auth": {
    "SeedAdminEmail": "admin@example.com",
    "Google": {
      "Enabled": true,
      "AuthenticationScheme": "Google",
      "ClientId": "<your-google-client-id>",
      "ClientSecret": "<your-google-client-secret>"
    }
  }
}
```

Environment variable examples:

- `Auth__Google__Enabled=true`
- `Auth__Google__ClientId=<your-google-client-id>`
- `Auth__Google__ClientSecret=<your-google-client-secret>`
- `Auth__SeedAdminEmail=admin@example.com`

### 1.12.2. Email / password

Local accounts are stored with a salted PBKDF2 password hash. The password policy requires at least 8 characters with uppercase, lowercase, digit, and special characters.

The Angular UI exposes the local sign-in form on `/login` and registration on `/register`.

### 1.12.3. Default administrator account

When `Auth:DefaultAdmin:Enabled` is `true`, the application seeds a built-in admin/editor account on startup if no user with the configured email exists. The development defaults are:

- Email: `admin@example.com`
- Password: `Admin123!`
- Roles: `Admin`, `Editor`

Override these in production:

```json
{
  "Auth": {
    "DefaultAdmin": {
      "Enabled": true,
      "Email": "admin@example.com",
      "Password": "<a-strong-password>",
      "Roles": ["Admin", "Editor"]
    }
  }
}
```

Environment variable examples:

- `Auth__DefaultAdmin__Enabled=true`
- `Auth__DefaultAdmin__Email=admin@example.com`
- `Auth__DefaultAdmin__Password=<a-strong-password>`
- `Auth__DefaultAdmin__Roles=Admin,Editor`

### 1.12.4. Local development and e2e tests without credentials

Enable the test login endpoint for a deterministic, credential-free session:

```json
{
  "Auth": {
    "Test": {
      "Enabled": true,
      "Email": "test@example.com",
      "DisplayName": "Test User",
      "Roles": ["Editor"]
    }
  }
}
```

Environment variable: `Auth__Test__Enabled=true`.

`POST /api/auth/test-login` is only available in non-Production environments and issues an authenticated cookie for the configured test user. The default `docker-compose.yml` ships with this enabled for e2e tests.

## 1.13. Known limitations

- No audit logging or audit columns.
- No soft deletes; records are physically removed.
- Role claims are captured at sign-in; a role change requires signing out and back in to refresh.
- No CI pipeline or deployment target beyond Docker Compose.

## 1.14. Improvements with more time

- Additional OAuth providers (Microsoft, GitHub) behind a small external-identity abstraction.
- Refresh role claims without forcing a full sign-out.
- Audit columns (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`).
- Soft deletes and a recycle-bin workflow.
- CI pipeline (build, test, container image publish).
- Richer Angular filtering/sorting (multi-column, debounced search).
- A React alternative version of the SPA for comparison, or Storybook for component documentation.
