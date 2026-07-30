## Purpose

Manages the book catalog: clients can create, search, update, and delete books, where each book belongs to exactly one author and one genre, and listings expose those relationships.

## ADDED Requirements

### Requirement: Create book

The system SHALL allow clients to create a book with a title, an author reference, a genre reference, an optional ISBN, and an optional publication year. The title SHALL be required, trimmed, and limited to 200 characters.

#### Scenario: Successful creation

- **WHEN** client submits a valid book referencing an existing author and genre
- **THEN** the book is persisted and the response returns `201 Created` with the created book (including its author and genre) and a `Location` header

#### Scenario: Missing or empty title

- **WHEN** client submits a title that is missing, empty, or whitespace-only
- **THEN** the request is rejected with a validation error identifying the `title` field

#### Scenario: Referenced author or genre does not exist

- **WHEN** client submits a book whose author identifier or genre identifier does not exist
- **THEN** the request is rejected with a validation error identifying the invalid reference field

### Requirement: Book relationship integrity

Every book SHALL belong to exactly one author and exactly one genre. Book representations returned by the system SHALL include the related author and genre (at minimum their identifiers and names).

#### Scenario: Book exposes its relationships

- **WHEN** client retrieves a book or a list of books
- **THEN** each book representation includes the name and identifier of its author and of its genre

### Requirement: ISBN validation and uniqueness

When an ISBN is provided, it SHALL be a valid ISBN-10 or ISBN-13 (after removing hyphens and spaces) and SHALL be unique across the catalog.

#### Scenario: Invalid ISBN format

- **WHEN** client submits a book with an ISBN that fails ISBN-10/ISBN-13 format or checksum validation
- **THEN** the request is rejected with a validation error identifying the `isbn` field

#### Scenario: Duplicate ISBN

- **WHEN** client submits a book with an ISBN already assigned to a different book
- **THEN** the request is rejected with a conflict error (`409`) explaining the ISBN is already in use

#### Scenario: ISBN omitted

- **WHEN** client submits a book without an ISBN
- **THEN** the book is accepted and stored with no ISBN

### Requirement: Publication year sanity

When a publication year is provided, it SHALL be between 1450 (Gutenberg-era lower bound) and the current calendar year, inclusive.

#### Scenario: Future publication year

- **WHEN** client submits a book with a publication year later than the current year
- **THEN** the request is rejected with a validation error identifying the `publishedYear` field

### Requirement: List, search, and sort books

The system SHALL provide a paginated list of books, optionally filtered by a case-insensitive partial title match, by author identifier, and by genre identifier (combinable), and sortable by title or publication year in ascending or descending order.

#### Scenario: Combined filtering

- **WHEN** client requests the book list with both a title filter and a genre identifier filter
- **THEN** only books matching both criteria are returned, inside the standard pagination envelope

#### Scenario: Sorted by publication year

- **WHEN** client requests the book list sorted by publication year descending
- **THEN** books are returned newest-first with stable ordering for ties

### Requirement: Get book by identifier

The system SHALL return a single book, including its author and genre, by its identifier.

#### Scenario: Book found

- **WHEN** client requests an existing book identifier
- **THEN** the response returns `200 OK` with the book including related author and genre

#### Scenario: Book not found

- **WHEN** client requests a book identifier that does not exist
- **THEN** the response returns `404 Not Found` following the standard error contract

### Requirement: Update book

The system SHALL allow clients to update all book fields, applying the same validation, reference-integrity, ISBN, and publication-year rules as creation.

#### Scenario: Successful update including re-parenting

- **WHEN** client updates an existing book with valid data, optionally changing its author and/or genre
- **THEN** the book is updated and the response returns the updated book with its new relationships

#### Scenario: Update non-existent book

- **WHEN** client updates a book identifier that does not exist
- **THEN** the response returns `404 Not Found`

### Requirement: Delete book

The system SHALL allow clients to delete any book. Deleting a book SHALL NOT affect its author or genre.

#### Scenario: Successful deletion

- **WHEN** client deletes an existing book
- **THEN** the book is removed, its author and genre remain intact, and the response returns `204 No Content`

#### Scenario: Delete non-existent book

- **WHEN** client deletes a book identifier that does not exist
- **THEN** the response returns `404 Not Found`
