# ADR 0008: Local Email/Password Authentication

## Status

Accepted

## Context

Philobiblos already supports Google OAuth, but some users cannot or do not want to use a Google account. The challenge asked for an alternative registration and login path so customers can create a local account with an email and password. A built-in administrator/editor account is also needed so the application can be used immediately after deployment without configuring OAuth credentials.

Key constraints:

- The backend is an ASP.NET Core 10 minimal API using Clean Architecture.
- Cookie-based sessions and policy-based authorization are already in place.
- Passwords must never be stored in plain text.
- The public API contract for entity endpoints must remain unchanged.
- The existing Google OAuth flow must remain intact.

## Decision

We will add a local email/password authentication option alongside Google OAuth.

- A nullable `PasswordHash` column is added to the `User` entity; local accounts populate it, OAuth accounts do not.
- Passwords are hashed with ASP.NET Core's `PasswordHasher<User>` (PBKDF2 with the framework's current defaults).
- Registration validates email uniqueness case-insensitively and enforces a password strength policy.
- `POST /api/auth/register` creates a local account and immediately signs the user in.
- `POST /api/auth/login` verifies the password and issues the same cookie session as OAuth.
- A default administrator/editor account is seeded on startup when `Auth:DefaultAdmin:Enabled` is `true` and no matching email exists.
- The Angular login page offers both Google sign-in and an email/password form, with a separate registration route.

## Consequences

### Positive

- Users can register without relying on an external identity provider.
- The same authorization policies (`Editor`, `Admin`) work for local and OAuth users.
- Built-in admin credentials make the app usable immediately after deployment.
- `PasswordHasher<User>` provides secure, salted hashing without adding EF Core Identity.

### Negative

- The application now stores sensitive credentials, increasing the security surface area.
- Default admin credentials must be overridden in production to avoid a known backdoor.
- There is no password reset or email verification flow yet.
- OAuth-only accounts cannot be promoted to local accounts without manual intervention.

## Alternatives considered

- **ASP.NET Core Identity with EF Core**: Provides a full identity system but adds significant schema and API complexity beyond the scope of this challenge.
- **BCrypt.NET**: A popular third-party hasher, but `PasswordHasher<User>` is already available and maintained by the framework.
- **JWT tokens instead of cookies**: Would require token storage decisions in the SPA and conflict with the existing cookie-based session.

## Related

- `backend/src/Philobiblos.Domain/Entities/User.cs`
- `backend/src/Philobiblos.Application/Users/Commands/RegisterUser.cs`
- `backend/src/Philobiblos.Application/Users/Commands/LoginUser.cs`
- `backend/src/Philobiblos.Infrastructure/HostedServices/DefaultAdminSeeder.cs`
- `backend/src/Philobiblos.Infrastructure/Security/AuthOptions.cs`
- `backend/src/Philobiblos.Api/Endpoints/AuthEndpoints.cs`
- `frontend/src/app/features/login/login.component.ts`
- `frontend/src/app/features/register/register.component.ts`
- `docs/adr/0006-oauth-authentication-with-google.md`
- `docs/adr/0007-opentelemetry-observability.md`
