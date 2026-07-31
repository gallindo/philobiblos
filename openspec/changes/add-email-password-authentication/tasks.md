## 1. Domain and data model

- [x] 1.1 Add a nullable `PasswordHash` property to `User` in `Philobiblos.Domain`.
- [x] 1.2 Add a case-insensitive unique index on `lower("Email")` in `LibraryDbContext`.
- [x] 1.3 Create an EF Core migration for the `User` schema changes.

## 2. Backend authentication

- [x] 2.1 Add `Auth:DefaultAdmin` configuration section with email, password, and roles.
- [x] 2.2 Add a `PasswordHasher<User>` registration and a password validation service.
- [x] 2.3 Implement `RegisterUserCommand` / `RegisterUserCommandHandler` with duplicate-email and password-strength checks.
- [x] 2.4 Implement `LoginUserCommand` / `LoginUserCommandHandler` that verifies the password and returns a `UserDto`.
- [x] 2.5 Add `POST /api/auth/register` and `POST /api/auth/login` endpoints in `AuthEndpoints`.
- [x] 2.6 Implement a default admin seeder that runs on startup and creates the admin account if it does not exist.
- [x] 2.7 Update `AuthOptionsValidator` to validate the default admin section when enabled.

## 3. Frontend

- [x] 3.1 Update the login component to show an email/password form in addition to the Google sign-in button.
- [x] 3.2 Add `register` and `loginWithEmailPassword` methods to `AuthService`.
- [x] 3.3 Add a registration route/component or inline registration form in the login flow.
- [x] 3.4 Surface server-side validation errors (duplicate email, weak password, invalid credentials) on the form controls.

## 4. Testing and validation

- [x] 4.1 Add unit tests for password hashing and validation rules.
- [x] 4.2 Add integration tests for successful registration, duplicate email rejection, weak password rejection, successful login, and invalid credentials.
- [x] 4.3 Add integration tests for the default admin seed.
- [x] 4.4 Run `dotnet test`, `npm test`, and `openspec validate --all`.

## 5. Documentation

- [x] 5.1 Add ADR 0008 documenting the email/password authentication choice.
- [x] 5.2 Update README.md with the new endpoints, default admin credentials, and how to override them.
- [x] 5.3 Update `docker-compose.yml` with default admin environment variables.
- [x] 5.4 Update `openspec/changes/add-email-password-authentication/tasks.md` as tasks are completed.
