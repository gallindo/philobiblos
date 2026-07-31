## Purpose

Make the identity and role claims of the currently authenticated user available to application-layer handlers without coupling them to ASP.NET Core authentication APIs.

## ADDED Requirements

### Requirement: Application layer can resolve the current user
The system SHALL provide a `ICurrentUser` abstraction that returns the authenticated user's id, email, display name, and roles when a request is authenticated, or `null` when the request is anonymous.

#### Scenario: Protected handler accesses current user
- **WHEN** a handler executes for a protected endpoint and the caller is authenticated
- **THEN** `ICurrentUser` returns the user's id, email, display name, and roles

#### Scenario: Anonymous read handler accesses current user
- **WHEN** a handler executes for an anonymous endpoint and the caller is not authenticated
- **THEN** `ICurrentUser` returns `null`

### Requirement: Infrastructure maps authentication claims to current user
The system SHALL implement `ICurrentUser` by reading claims from the authenticated `ClaimsPrincipal` and translating them into the application-level user identity.

#### Scenario: Request with valid cookie reaches a handler
- **WHEN** a request with a valid authentication cookie is processed
- **THEN** the handler receives an `ICurrentUser` populated from the cookie claims

### Requirement: Handlers can read the current user for future auditing
The system SHALL make the current user's id available to handlers so that future audit logging can attribute write operations to the authenticated user.

> **Note:** Recording audit metadata (e.g., `CreatedBy`, `UpdatedBy`) is not implemented in this version; the current user identity is exposed only for authorization and the `/api/auth/me` endpoint.

#### Scenario: Handler reads the current user id
- **WHEN** a handler executes for a protected endpoint and the caller is authenticated
- **THEN** `ICurrentUser.Id` returns the authenticated user's id
- **AND** the handler can use that id for future audit logging
