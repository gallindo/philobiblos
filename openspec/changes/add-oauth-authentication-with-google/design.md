## Context

Philobiblos is a .NET 10 minimal-API backend with a Clean Architecture split (`Domain` → `Application` → `Infrastructure` → `Api`) and an Angular 19 SPA frontend. The public API currently exposes full CRUD for genres, authors, and books with no authentication. See `proposal.md` for the motivation and high-level scope.

## Goals / Non-Goals

**Goals:**
- Add Google OAuth as an external login option and maintain a cookie-based session.
- Introduce role-based authorization (`Editor`, `Admin`) and protect write endpoints.
- Keep read endpoints anonymous.
- Provide a clean `ICurrentUser` abstraction to the application layer.
- Update the Angular frontend with login/logout and role-aware UI.
- Add migrations, tests, and an ADR documenting the strategy.

**Non-Goals:**
- Password-based local accounts.
- JWT bearer tokens or token refresh logic.
- Fine-grained resource-level authorization (e.g., "can edit only your own books").
- Full audit columns on entities in this change (only the audit context service is introduced).
- OpenTelemetry metrics/traces beyond existing logging.

## Decisions

### 1. Cookie authentication instead of JWT
**Choice:** Use ASP.NET Core Cookie Authentication with the Google OAuth handler.
**Rationale:** The SPA is served from the same origin via nginx and calls `/api/*` cookies naturally. Cookies avoid storing tokens in browser storage and eliminate token refresh logic.
**Alternative:** JWT in `localStorage` — rejected because it is more vulnerable to XSS and requires refresh-token handling.

### 2. Google identity stored by provider subject
**Choice:** The `User` entity is keyed by `Provider` + `ProviderSubject`. Email is kept up to date on each login but does not uniquely identify the account.
**Rationale:** Allows users to change their Google email without breaking their Philobiblos account.

### 3. Roles resolved from the database on every request
**Choice:** The authentication cookie stores only identity claims (`sub`, `email`, `name`). A scoped `ICurrentUser` service loads roles from the database using the subject claim.
**Rationale:** Role changes made by an admin take effect immediately without forcing users to re-authenticate.
**Trade-off:** One extra database query per authenticated request. Acceptable for this scale and can be cached later if needed.

### 4. `ICurrentUser` abstraction in the application layer
**Choice:** Application handlers depend on an `ICurrentUser` interface implemented in Infrastructure by reading `IHttpContextAccessor.HttpContext.User`.
**Rationale:** Keeps application logic free of ASP.NET Core authentication types while still making identity available for authorization checks and attribution.

### 5. Authorization policies applied at the endpoint level
**Choice:** Use `RequireAuthorization("Editor")` on write endpoints and leave read endpoints anonymous via `[AllowAnonymous]` or by not requiring auth.
**Rationale:** Minimal change to existing handlers; the auth decision is declared where routes are mapped.

### 6. First signed-in user does not auto-promote
**Choice:** Role assignment requires an existing `Admin` to call the role-management endpoint.
**Rationale:** Avoids implicit privilege escalation and forces explicit admin configuration.
**Open handling:** A bootstrap admin email can be set via configuration (`Auth:SeedAdminEmail`) so the first deployment can pre-authorize an administrator without database edits.

## Risks / Trade-offs

- **OAuth secret management** → Mitigation: store `Auth:Google:ClientId` and `Auth:Google:ClientSecret` in environment variables / secrets, never in `appsettings.json` for non-local environments.
- **Google callback URI mismatch** → Mitigation: configure the callback path (`/api/auth/callback`) explicitly and document the authorized redirect URI in the setup guide.
- **Existing write endpoints become breaking changes** → Mitigation: clearly document that `POST`, `PUT`, and `DELETE` now require an `Editor` role; provide the seed-admin flow for first setup.
- **Integration testing OAuth** → Mitigation: add a fake Google handler in the integration test project using `AuthenticationScheme` replacement so tests can sign in without real Google credentials.
- **Anonymous read access may be undesired later** → Mitigation: policies are centralized; switching reads to require authentication is a one-line change per endpoint group.

## Migration Plan

1. Add the `Users` table migration and run it against the existing database.
2. Configure Google OAuth credentials and set `Auth:SeedAdminEmail` for the first administrator.
3. Deploy the backend and frontend images.
4. The first admin signs in via Google, is granted `Admin` role by the seed config, then promotes additional editors through the UI or API.
5. Rollback: revert to the previous Docker image and, if necessary, remove the `Users` migration.

## Open Questions

- Should the cookie sliding expiration be 7 days or 14 days? This affects convenience vs. security but does not change the approach.
- Should the SPA show a "Log in with Google" button on every page or only on a dedicated `/login` route? Defer to frontend implementation.
