## Context

See `proposal.md` for motivation. The backend already has cookie-based authentication, Google OAuth, and a `User` entity. The current `User` entity tracks email, provider, provider subject, and role, but it has no password storage. This design adds a local email/password path while reusing the existing cookie session and role-based authorization.

## Goals / Non-Goals

**Goals:**
- Add a local account option (email + password) alongside Google OAuth.
- Store passwords using a trusted, salted one-way hash.
- Provide backend registration and login endpoints.
- Seed a default admin/editor account on startup so the app is usable without Google credentials.
- Update the Angular login UI to offer both sign-in methods.
- Add tests covering registration, login, password hashing, and the admin seed.

**Non-Goals:**
- Account linking between OAuth and local accounts.
- Password reset or "forgot password" flow.
- Email verification.
- Migrating existing OAuth users to local accounts.
- Changing the role model or authorization policies beyond the existing `Editor` and `Admin` roles.

## Decisions

### Use `PasswordHasher<User>` from ASP.NET Core Identity
**Rationale:** It is included in the shared framework, requires no extra EF Core Identity package, and implements PBKDF2 with reasonable defaults.
**Alternative considered:** BCrypt.NET — adds a third-party dependency and offers no clear advantage over the built-in hasher for this scope.

### Store a nullable `PasswordHash` on the `User` entity
**Rationale:** Local users have a password hash; OAuth users do not. A nullable column keeps the two paths explicit and avoids inventing a password for OAuth users.
**Alternative considered:** Separate `LocalCredentials` table — adds an unnecessary join for a 1:1 relationship.

### Add a case-insensitive unique index on `lower("Email")`
**Rationale:** Prevents duplicate registrations with differently cased emails and matches PostgreSQL's case-insensitive uniqueness pattern used elsewhere.

### Issue the same cookie session for local users
**Rationale:** Reuses the existing `SignInUserAsync` helper and authorization policies; local users are indistinguishable from OAuth users after sign-in.

### Seed the default admin via a scoped startup service
**Rationale:** Runs after migrations, checks for the configured email, and creates a hashed password only when the account does not exist. Keeps the seed logic testable and environment-aware.

### Configure the default admin through `Auth:DefaultAdmin`
**Rationale:** Keeps credentials out of source code and lets operators override them through `appsettings.json` or environment variables.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Default admin credentials left unchanged in production | README and logs strongly recommend overriding `Auth:DefaultAdmin:Password` and using a different admin account in production. |
| Email collision between OAuth and local accounts | Registration checks for an existing email case-insensitively and rejects the request with 409. |
| Password hash algorithm becomes outdated | `PasswordHasher<User>` uses ASP.NET Core's current defaults; future framework updates can be adopted by rotating hashes at next login. |
| Frontend UI becomes more complex with two auth paths | Login component uses a toggle between "Sign in with Google" and the email/password form. |

## Migration Plan

1. Add `PasswordHash` to `User` and a unique index on lower-cased email.
2. Create an EF Core migration.
3. Implement backend registration, login, and admin seed.
4. Update the Angular login component and `AuthService`.
5. Add unit and integration tests.
6. Update ADRs, README, and `docker-compose.yml` with default admin environment variables.
7. Run `dotnet test`, `npm test`, and `openspec validate --all`.

## Open Questions

- None.
