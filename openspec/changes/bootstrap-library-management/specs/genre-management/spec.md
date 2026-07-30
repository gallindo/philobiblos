## Purpose

Manages book genres as an independent catalog: clients can create, search, update, and delete genres, with integrity rules that keep the catalog consistent for the books that reference it.

## ADDED Requirements

### Requirement: Create genre

The system SHALL allow clients to create a genre by providing a name. The name SHALL be required, trimmed of surrounding whitespace, and limited to 100 characters.

#### Scenario: Successful creation

- **WHEN** client submits a valid genre name
- **THEN** the genre is persisted and the response returns `201 Created` with the created genre and a `Location` header pointing to its resource

#### Scenario: Missing or empty name

- **WHEN** client submits a name that is missing, empty, or whitespace-only
- **THEN** the request is rejected with a validation error identifying the `name` field

#### Scenario: Name too long

- **WHEN** client submits a name longer than 100 characters
- **THEN** the request is rejected with a validation error identifying the `name` field

### Requirement: Genre name uniqueness

Genre names SHALL be unique, compared case-insensitively and ignoring surrounding whitespace.

#### Scenario: Duplicate name rejected

- **WHEN** client creates or updates a genre with a name that matches an existing genre under case-insensitive comparison
- **THEN** the request is rejected with a conflict error (`409`) explaining the name is already in use

### Requirement: List and search genres

The system SHALL provide a paginated list of genres, optionally filtered by a case-insensitive partial name match, and sortable by name in ascending or descending order.

#### Scenario: Filtered listing

- **WHEN** client requests the genre list with a name filter
- **THEN** only genres whose names contain the filter text (case-insensitive) are returned, inside the standard pagination envelope

#### Scenario: Sorted listing

- **WHEN** client requests the genre list sorted by name descending
- **THEN** genres are returned in descending name order with stable ordering for ties

### Requirement: Get genre by identifier

The system SHALL return a single genre by its identifier.

#### Scenario: Genre found

- **WHEN** client requests an existing genre identifier
- **THEN** the response returns `200 OK` with the genre

#### Scenario: Genre not found

- **WHEN** client requests a genre identifier that does not exist
- **THEN** the response returns `404 Not Found` following the standard error contract

### Requirement: Update genre

The system SHALL allow clients to update a genre's name, applying the same validation and uniqueness rules as creation.

#### Scenario: Successful update

- **WHEN** client updates an existing genre with a valid, non-conflicting name
- **THEN** the genre is updated and the response returns the updated genre

#### Scenario: Update non-existent genre

- **WHEN** client updates a genre identifier that does not exist
- **THEN** the response returns `404 Not Found`

### Requirement: Delete genre with referential protection

The system SHALL allow deleting a genre only when no books reference it.

#### Scenario: Successful deletion

- **WHEN** client deletes a genre that has no associated books
- **THEN** the genre is removed and the response returns `204 No Content`

#### Scenario: Genre in use

- **WHEN** client deletes a genre that has one or more associated books
- **THEN** the genre is preserved and the response returns a conflict error (`409`) explaining the genre is in use

#### Scenario: Delete non-existent genre

- **WHEN** client deletes a genre identifier that does not exist
- **THEN** the response returns `404 Not Found`
