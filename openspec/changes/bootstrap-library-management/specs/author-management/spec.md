## Purpose

Manages book authors as an independent catalog: clients can create, search, update, and delete authors, with integrity rules that keep the catalog consistent for the books that reference it.

## ADDED Requirements

### Requirement: Create author

The system SHALL allow clients to create an author by providing a name and an optional biography. The name SHALL be required, trimmed of surrounding whitespace, and limited to 150 characters. The biography, when provided, SHALL be limited to 2000 characters.

#### Scenario: Successful creation

- **WHEN** client submits a valid author name with or without a biography
- **THEN** the author is persisted and the response returns `201 Created` with the created author and a `Location` header pointing to its resource

#### Scenario: Missing or empty name

- **WHEN** client submits a name that is missing, empty, or whitespace-only
- **THEN** the request is rejected with a validation error identifying the `name` field

#### Scenario: Field too long

- **WHEN** client submits a name over 150 characters or a biography over 2000 characters
- **THEN** the request is rejected with a validation error identifying the offending field

### Requirement: Author name uniqueness

Author names SHALL be unique, compared case-insensitively and ignoring surrounding whitespace.

#### Scenario: Duplicate name rejected

- **WHEN** client creates or updates an author with a name that matches an existing author under case-insensitive comparison
- **THEN** the request is rejected with a conflict error (`409`) explaining the name is already in use

### Requirement: List and search authors

The system SHALL provide a paginated list of authors, optionally filtered by a case-insensitive partial name match, and sortable by name in ascending or descending order.

#### Scenario: Filtered listing

- **WHEN** client requests the author list with a name filter
- **THEN** only authors whose names contain the filter text (case-insensitive) are returned, inside the standard pagination envelope

#### Scenario: Sorted listing

- **WHEN** client requests the author list sorted by name descending
- **THEN** authors are returned in descending name order with stable ordering for ties

### Requirement: Get author by identifier

The system SHALL return a single author by its identifier.

#### Scenario: Author found

- **WHEN** client requests an existing author identifier
- **THEN** the response returns `200 OK` with the author

#### Scenario: Author not found

- **WHEN** client requests an author identifier that does not exist
- **THEN** the response returns `404 Not Found` following the standard error contract

### Requirement: Update author

The system SHALL allow clients to update an author's name and biography, applying the same validation and uniqueness rules as creation.

#### Scenario: Successful update

- **WHEN** client updates an existing author with valid, non-conflicting data
- **THEN** the author is updated and the response returns the updated author

#### Scenario: Update non-existent author

- **WHEN** client updates an author identifier that does not exist
- **THEN** the response returns `404 Not Found`

### Requirement: Delete author with referential protection

The system SHALL allow deleting an author only when no books reference them.

#### Scenario: Successful deletion

- **WHEN** client deletes an author who has no associated books
- **THEN** the author is removed and the response returns `204 No Content`

#### Scenario: Author in use

- **WHEN** client deletes an author who has one or more associated books
- **THEN** the author is preserved and the response returns a conflict error (`409`) explaining the author is in use

#### Scenario: Delete non-existent author

- **WHEN** client deletes an author identifier that does not exist
- **THEN** the response returns `404 Not Found`
