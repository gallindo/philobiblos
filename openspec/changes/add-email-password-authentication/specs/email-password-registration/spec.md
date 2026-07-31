## Purpose

Lets users create a local account with an email and password so they can sign in without relying on an external OAuth provider.

## ADDED Requirements

### Requirement: Registration creates a local account
The system SHALL allow an anonymous user to register a local account by submitting an email address and a password.

#### Scenario: Successful registration
- **WHEN** an anonymous user submits a valid email and a password that meets the strength policy
- **THEN** the system creates a new user with the email and a password hash
- **THEN** the system returns a success response
- **THEN** the user can sign in with the same email and password

#### Scenario: Duplicate email is rejected
- **WHEN** an anonymous user submits a registration request with an email already associated with an existing local or OAuth account
- **THEN** the system returns a 409 Conflict response without creating a new account

#### Scenario: Weak password is rejected
- **WHEN** an anonymous user submits a password that does not meet the strength policy
- **THEN** the system returns a 400 Bad Request response with a clear validation error
- **THEN** no account is created

### Requirement: Passwords are never stored in plain text
The system MUST store a salted, one-way hash of the password instead of the password itself.

#### Scenario: Stored password is a hash
- **WHEN** a user registers with a password
- **THEN** the database record contains a password hash derived from the password
- **THEN** the original password cannot be recovered from the stored value
