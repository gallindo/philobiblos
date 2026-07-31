## Why

Philobiblos currently only supports Google OAuth for authentication. End users who cannot or do not want to use a Google account have no way to register or sign in. Adding a local email/password option removes that friction while keeping Google OAuth as an alternative for users who prefer it.

## What Changes

- Add email/password registration and sign-in as an alternative authentication path.
- Extend the `User` entity and database schema to store a password hash for locally registered accounts.
- Add backend endpoints for registration (`POST /api/auth/register`) and login (`POST /api/auth/login`).
- Seed a default administrator/editor account on startup so the application is usable immediately after deployment.
- Update the Angular login UI to let users choose between Google OAuth and email/password sign-in.
- Add unit and integration tests for the new auth flows, password hashing, and the default admin seed.
- Update ADRs and README to document the local authentication option and the default admin credentials.

## Capabilities

### New Capabilities

- `email-password-registration`: Users can create a local account by providing an email and a password.
- `email-password-login`: Users can sign in with an email and password and receive the same cookie session as OAuth users.
- `default-admin-seed`: The application creates a built-in administrator/editor account on startup when no matching account exists.

### Modified Capabilities

- None. Google OAuth behavior remains unchanged; this is an additive alternative.

## Impact

- Backend: `User` entity, `LibraryDbContext`, EF Core migration, `AuthOptions`, auth endpoints, password hashing, and seed logic.
- Frontend: login component and `AuthService` to support email/password forms.
- Database schema: new `PasswordHash` column on `Users`.
- No breaking changes to the existing public API contract.
