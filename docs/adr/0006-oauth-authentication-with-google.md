# ADR 0006: OAuth 2.0 Authentication with Google and Cookie Sessions

## Status

Accepted

## Context

Philobiblos needs to distinguish anonymous visitors from authenticated editors and administrators so that write operations (create, update, delete) are not open to the public. The challenge asked for OAuth with Google as a login option, which is a common choice for small internal tools because it delegates credential management to a trusted provider.

Key constraints:

- The backend is an ASP.NET Core 10 minimal API.
- The frontend is an Angular SPA served by nginx and proxied to the API.
- We want to avoid storing passwords or managing token lifetimes ourselves.
- Existing read endpoints must remain available to anonymous users.
- The public HTTP contract for the original entity endpoints should remain unchanged.
- End-to-end tests run against the full Docker Compose stack and cannot depend on real Google credentials.

## Decision

We will use **Google OAuth 2.0** for identity proofing and **ASP.NET Core Cookie Authentication** for session management.

- The backend registers the Google handler only when `Auth:Google:Enabled` is `true`.
- After a successful OAuth callback, the backend creates or updates a `User` record and issues a persistent, HttpOnly, SameSite=Lax authentication cookie.
- Role information is stored in the authentication cookie claims at sign-in time and enforced via policy-based authorization (`Editor` and `Admin`).
- A `Test` authentication scheme is available in non-Production environments when `Auth:Test:Enabled` is `true`; it exposes `POST /api/auth/test-login` so tests and local demos can obtain an authenticated session without Google credentials.
- Write endpoints require the `Editor` policy; an admin-only endpoint allows role changes.
- The frontend loads the current user from `GET /api/auth/me`, hides write actions for anonymous users, and redirects to `/login` on 401 API responses.

## Consequences

### Positive

- No passwords are stored in the application database.
- Session handling is delegated to the battle-tested cookie authentication middleware.
- Roles are simple enum values on the `User` entity; claims are derived at sign-in.
- The test login endpoint keeps CI and local e2e tests deterministic and fast.

### Negative

- Cookie sessions require careful CORS/SameSite configuration if the SPA and API ever run on different origins.
- The application is tied to Google's OAuth flow; supporting additional providers would require handler registration and a small abstraction over the external identity.
- `ICurrentUser` reads roles from the cookie claims, which can become stale if a user's role is changed while they are logged in; the user must sign out and back in to refresh claims.

## Alternatives considered

- **JWT Bearer tokens**: More common for SPAs, but adds token refresh logic and storage decisions (localStorage vs. httpOnly cookies). Cookies fit the same-origin proxy setup and avoid XSS-prone token storage.
- **IdentityServer / OpenIddict**: Overkill for a single-provider, small-scope challenge.
- **API keys / basic auth**: Would require password management and contradicts the OAuth requirement.

## Related

- `backend/src/Philobiblos.Domain/Entities/User.cs`
- `backend/src/Philobiblos.Infrastructure/Security/AuthOptions.cs`
- `backend/src/Philobiblos.Infrastructure/DependencyInjection.cs`
- `backend/src/Philobiblos.Api/Endpoints/AuthEndpoints.cs`
- `frontend/src/app/core/auth.service.ts`
- `docs/adr/0004-error-contract.md`
