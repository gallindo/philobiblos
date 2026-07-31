## Why

Philobiblos currently has no authentication or authorization, so every user can create, update, or delete library records. As the system grows, we need to know who is making changes and restrict destructive operations to trusted users. Google OAuth provides a low-friction, secure login option without forcing users to manage another password.

## What Changes

- Add a **User** entity and repository to store the authenticated user's external identity, email, and role.
- Integrate **ASP.NET Core Authentication with Google OAuth** as a login option.
- Use **cookie-based sessions** for the backend and align the Angular SPA to initiate the OAuth flow.
- Add **authorization policies** (e.g., `Admin`, `Editor`) and protect write endpoints (`POST`, `PUT`, `DELETE`) while leaving read endpoints (`GET`) open to anonymous users.
- Add a **current-user service** so application handlers can stamp changes with the actor's id.
- Add login/logout UI flows in the Angular frontend and guard protected actions.
- Update tests to cover the new auth plumbing (unit tests for authorization rules, integration tests for the OAuth callback contract).
- Add ADR documenting the auth strategy and provider choice.

## Capabilities

### New Capabilities

- `oauth-authentication`: External authentication via Google OAuth, cookie session management, and sign-in/sign-out endpoints.
- `authorization-policies`: Role-based access control with policies that protect write endpoints and expose a read-only public API.
- `current-user-context`: Propagating the authenticated user's identity into the application layer so handlers can attribute actions.

### Modified Capabilities

- None.

## Impact

- Backend: new `Philobiblos.Domain` user entity + repository interfaces; new `Application` commands/queries for login/logout; new `Infrastructure` OAuth + cookie auth services; endpoint authorization in `Philobiblos.Api`.
- Frontend: new auth service, login button, route guards, and UI state for authenticated user.
- Database: new migration adding the `Users` table with unique indexes on external provider identity.
- Dependencies: `Microsoft.AspNetCore.Authentication.Google` and `Microsoft.AspNetCore.Authentication.Cookies`.
- Public API: new `/api/auth/*` routes; existing GET routes remain anonymous; existing write routes become `Authorization` protected.
- Docker: Google OAuth client id/secret must be supplied via environment variables; the `AllowedHosts` and cookie policy settings will be tightened for production.
