## Purpose

Lets users sign in with an email and password to obtain the same cookie-based session used by OAuth users.

## ADDED Requirements

### Requirement: Email and password sign-in issues a session
The system SHALL authenticate a user when they provide a registered email and the correct password, and issue the standard authentication cookie.

#### Scenario: Successful sign-in
- **WHEN** a user submits a valid email and the matching password
- **THEN** the system returns a success response
- **THEN** the system issues an HTTP-only authentication cookie containing the user's identity and roles
- **THEN** the user is treated as authenticated for subsequent requests

#### Scenario: Unknown email is rejected
- **WHEN** a user submits an email that is not registered locally
- **THEN** the system returns a 401 Unauthorized response
- **THEN** no authentication cookie is issued

#### Scenario: Wrong password is rejected
- **WHEN** a user submits a registered email with an incorrect password
- **THEN** the system returns a 401 Unauthorized response
- **THEN** no authentication cookie is issued

#### Scenario: OAuth-only accounts cannot sign in with a password
- **WHEN** a user created through Google OAuth tries to sign in with a password
- **THEN** the system returns a 401 Unauthorized response
- **THEN** no authentication cookie is issued
