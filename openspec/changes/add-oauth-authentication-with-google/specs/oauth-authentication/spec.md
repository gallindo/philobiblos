## Purpose

Authenticate users via Google OAuth and maintain a secure server-side session so that the Angular SPA can access protected API endpoints on behalf of the signed-in user.

## ADDED Requirements

### Requirement: Login endpoint initiates Google OAuth flow
The system SHALL expose an HTTP endpoint that, when Google OAuth is enabled (`Auth:Google:Enabled=true`), challenges the caller with Google OAuth and requests the `openid`, `email`, and `profile` scopes.

#### Scenario: User requests login with Google OAuth enabled
- **WHEN** an unauthenticated user navigates to `/api/auth/login`
- **THEN** the backend redirects the browser to Google's OAuth consent screen

#### Scenario: User requests login with Google OAuth disabled
- **WHEN** Google OAuth is disabled and a user navigates to `/api/auth/login`
- **THEN** the backend returns `503 Service Unavailable` with a clear error message
- **AND** no redirect to Google occurs

### Requirement: OAuth callback creates or updates a local user
The system SHALL validate the Google identity token returned to `/api/auth/callback`, create or update a local `User` record keyed by the external provider identity, and issue an authentication cookie.

#### Scenario: First-time Google sign-in
- **WHEN** a user completes Google OAuth for the first time
- **THEN** the system creates a `User` record with the user's email, provider name, provider subject, default role, and sign-in timestamp
- **AND** issues an authentication cookie
- **AND** redirects the browser to the SPA home page

#### Scenario: Returning Google sign-in
- **WHEN** a user completes Google OAuth and the provider subject already exists
- **THEN** the system updates the existing `User` record's email and last sign-in timestamp
- **AND** issues an authentication cookie
- **AND** redirects the browser to the SPA home page

#### Scenario: Google denies consent
- **WHEN** Google returns an error or the user denies consent
- **THEN** the system redirects the browser to the SPA login page with a readable failure message
- **AND** no authentication cookie is issued

### Requirement: Logout endpoint terminates the session
The system SHALL expose an HTTP endpoint that signs the user out and clears the authentication cookie.

#### Scenario: Authenticated user logs out
- **WHEN** an authenticated user calls `/api/auth/logout`
- **THEN** the authentication cookie is removed
- **AND** subsequent requests are treated as anonymous

### Requirement: Session cookie is secure by default
The system SHALL configure the authentication cookie as `HttpOnly` and `SameSite=Lax`; the `Secure` flag SHALL be applied according to the request scheme so that cookies are only transmitted over HTTPS when the API is served over HTTPS.

#### Scenario: Cookie is issued after successful login
- **WHEN** the system issues the authentication cookie
- **THEN** the cookie has `HttpOnly=true`, `SameSite=Lax`, and an expiration matching the session lifetime

### Requirement: Authenticated user info is exposed to the SPA
The system SHALL expose an endpoint that returns the current user's id, email, name, and roles, or indicates that the caller is anonymous.

#### Scenario: SPA requests current user while authenticated
- **WHEN** the SPA calls `/api/auth/me` with a valid cookie
- **THEN** the response contains the user's id, email, display name, and roles

#### Scenario: SPA requests current user while anonymous
- **WHEN** the SPA calls `/api/auth/me` without a valid cookie
- **THEN** the response indicates the caller is anonymous
