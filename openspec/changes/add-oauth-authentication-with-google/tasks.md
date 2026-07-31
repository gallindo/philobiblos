## 1. Domain and data model

- [x] 1.1 Add `User` entity and `Role` enum to `Philobiblos.Domain`.
- [x] 1.2 Add `IUserRepository` interface with `GetByProviderAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, and `AnyAsync`.
- [x] 1.3 Add `ICurrentUser` interface returning id, email, display name, and roles.

## 2. Infrastructure authentication and authorization

- [x] 2.1 Add `Microsoft.AspNetCore.Authentication.Google` and `Microsoft.AspNetCore.Authentication.Cookies` packages to `Philobiblos.Infrastructure`.
- [x] 2.2 Implement `UserRepository` in `Philobiblos.Infrastructure`.
- [x] 2.3 Implement `HttpContextCurrentUser` in `Philobiblos.Infrastructure` using `IHttpContextAccessor`.
- [x] 2.4 Add auth configuration section (`Auth:Google:ClientId`, `Auth:Google:ClientSecret`, `Auth:Cookie:...`, `Auth:SeedAdminEmail`) and validation.
- [x] 2.5 Add DI extension `AddAuthenticationAndAuthorization` registering cookie + Google auth, policies, and `IHttpContextAccessor`/`ICurrentUser`.
- [x] 2.6 Add `User` EF Core configuration and migration.
- [x] 2.7 Implement seed-admin logic: if `SeedAdminEmail` matches the authenticated user's email, grant `Admin` role on first sign-in.

## 3. Application auth commands and queries

- [x] 3.1 Add `GetOrCreateUserCommand` and handler that creates/updates the user from Google claims and returns the user DTO.
- [x] 3.2 Add `GetCurrentUserQuery` and handler that returns the current user or anonymous indicator.
- [x] 3.3 Add `SignOutUserCommand` and handler (no-op handler, used for endpoint clarity).
- [x] 3.4 Add `UpdateUserRolesCommand` and handler restricted by the `Admin` policy at the endpoint.

## 4. API endpoints and authorization

- [x] 4.1 Add `AuthEndpoints` mapping `/api/auth/login`, `/api/auth/callback`, `/api/auth/logout`, `/api/auth/me`, and `/api/auth/users/{id}/roles`.
- [x] 4.2 Apply `RequireAuthorization("Editor")` to all write endpoints (`POST`, `PUT`, `DELETE`) for genres, authors, and books.
- [x] 4.3 Apply `RequireAuthorization("Admin")` to the user role-management endpoint.
- [x] 4.4 Wire `AddAuthenticationAndAuthorization` in `Program.cs` before `AddApplication`/`AddInfrastructure`.
- [x] 4.5 Update `appsettings.Development.json` to use local auth secrets and a seed admin email.

## 5. Frontend auth integration

- [x] 5.1 Add `AuthService` in Angular with `me$`, `login()`, `logout()`, and role helpers.
- [x] 5.2 Add a login button to the navigation shell and a `/login` route.
- [x] 5.3 Hide create/edit/delete buttons when the user is not authenticated or lacks the `Editor` role.
- [x] 5.4 Add an HTTP interceptor that handles 401/403 responses and redirects to `/login` or shows a permission message.
- [x] 5.5 Update `proxy.conf.json` to pass cookies to the backend for the OAuth callback flow.

## 6. Migrations and operational updates

- [x] 6.1 Generate the `Users` migration from `Philobiblos.Infrastructure`.
- [x] 6.2 Update `docker-compose.yml` to accept `Auth__Google:ClientId`, `Auth__Google:ClientSecret`, and `Auth__SeedAdminEmail` environment variables.
- [x] 6.3 Update `README.md` with setup instructions for Google OAuth and seed admin.

## 7. Tests, ADR, and validation

- [x] 7.1 Add unit tests for `HttpContextCurrentUser` mapping and `GetOrCreateUserCommand` role assignment.
- [x] 7.2 Add a fake Google OAuth authentication handler for integration tests.
- [x] 7.3 Add integration tests for login, `/api/auth/me`, write-endpoint authorization, and admin role management.
- [x] 7.4 Add `docs/adr/0006-oauth-authentication.md` documenting the cookie + Google OAuth choice.
- [x] 7.5 Run `dotnet test`, `npm run test`, and `openspec validate`.
- [x] 7.6 Run `docker compose up --build` and verify the OAuth flow manually (or with a staged test account).
