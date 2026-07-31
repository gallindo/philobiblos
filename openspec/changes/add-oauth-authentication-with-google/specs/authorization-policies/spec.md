## Purpose

Enforce role-based access control on API endpoints so that destructive operations are restricted to authenticated users with appropriate roles while read access remains open to the public.

## ADDED Requirements

### Requirement: Authorization policies are defined
The system SHALL define authorization policies named `Editor` and `Admin` backed by role claims stored on the authenticated user.

#### Scenario: Authenticated user has Editor role
- **WHEN** the current user has the `Editor` role claim
- **THEN** the user satisfies the `Editor` authorization policy

#### Scenario: Authenticated user has Admin role
- **WHEN** the current user has the `Admin` role claim
- **THEN** the user satisfies both the `Admin` and `Editor` authorization policies

### Requirement: Write endpoints require an authenticated editor
The system SHALL require the `Editor` policy on all endpoints that create, update, or delete genres, authors, or books.

#### Scenario: Editor creates a genre
- **WHEN** an authenticated user with the `Editor` role posts to `/api/genres`
- **THEN** the genre is created and the response status is `201 Created`

#### Scenario: Anonymous user attempts to create a genre
- **WHEN** an unauthenticated user posts to `/api/genres`
- **THEN** the response status is `401 Unauthorized` and no genre is created

#### Scenario: Non-editor attempts to delete a book
- **WHEN** an authenticated user without the `Editor` role deletes `/api/books/{id}`
- **THEN** the response status is `403 Forbidden` and the book is not deleted

### Requirement: Read endpoints remain anonymous
The system SHALL allow unauthenticated access to all list and get endpoints for genres, authors, and books.

#### Scenario: Anonymous user lists books
- **WHEN** an unauthenticated user gets `/api/books`
- **THEN** the response status is `200 OK` and the paginated book list is returned

### Requirement: Role management is restricted to administrators
The system SHALL expose endpoints for viewing and updating user roles and require the `Admin` policy.

#### Scenario: Admin promotes a user to Editor
- **WHEN** an authenticated `Admin` user patches `/api/auth/users/{id}/roles` with `Editor`
- **THEN** the user's role is updated and the response status is `200 OK`

#### Scenario: Non-admin attempts to promote a user
- **WHEN** a non-admin authenticated user patches `/api/auth/users/{id}/roles`
- **THEN** the response status is `403 Forbidden` and the user's role is unchanged
