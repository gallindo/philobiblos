## Purpose

Provides a built-in administrator/editor account so the application is usable for management immediately after deployment without requiring Google OAuth configuration.

## ADDED Requirements

### Requirement: Application seeds a default administrator on startup
The system SHALL create a default administrator/editor account on startup if no account with the configured admin email exists.

#### Scenario: Fresh database receives the default admin
- **WHEN** the application starts against an empty database
- **THEN** the system creates a local user with the configured admin email and a hashed default password
- **THEN** the user is assigned both the Admin and Editor roles
- **THEN** the user can sign in with the configured email and password

#### Scenario: Existing account is left unchanged
- **WHEN** the application starts and a user with the configured admin email already exists
- **THEN** the system does not modify the existing user's password or roles

### Requirement: Default admin credentials are configurable
The system MUST read the default administrator email and password from configuration.

#### Scenario: Custom admin credentials via configuration
- **WHEN** an operator provides a custom admin email and password via configuration or environment variables
- **THEN** the seeded account uses those values
