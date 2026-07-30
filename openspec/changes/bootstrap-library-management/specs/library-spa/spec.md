## Purpose

Provides a browser-based single-page application where users manage the genre, author, and book catalogs end-to-end and can see how each book relates to its author and genre.

## ADDED Requirements

### Requirement: Catalog navigation

The application SHALL offer clear navigation between three management sections — genres, authors, and books — each reachable within one interaction from anywhere in the app.

#### Scenario: Switching sections

- **WHEN** user selects a different section in the navigation
- **THEN** the corresponding management view is displayed without a full page reload

### Requirement: List views with search and pagination

Each section SHALL present its records in a list with a text search (where the API supports it) and paged navigation through the catalog.

#### Scenario: Searching a catalog

- **WHEN** user types into the search field of a section
- **THEN** the list refreshes to show only matching records with pagination reset to the first page

#### Scenario: Paging through results

- **WHEN** user navigates to another page of a list
- **THEN** the list displays the records for that page along with the current page position and total record count

### Requirement: Register and edit records

Each section SHALL provide forms to create new records and edit existing ones, with client-side required-field indication and server-side validation errors displayed next to the offending fields.

#### Scenario: Server validation errors surfaced per field

- **WHEN** user submits a form that fails server validation
- **THEN** the form displays the server-provided message for each failing field without losing the user's input

### Requirement: Remove records with confirmation

Each section SHALL allow deleting a record only after an explicit user confirmation, and SHALL surface referential-conflict errors as understandable messages.

#### Scenario: Delete confirmed

- **WHEN** user confirms deletion of a record with no blocking relationships
- **THEN** the record disappears from the list and a success indication is shown

#### Scenario: Delete blocked by relationships

- **WHEN** user confirms deletion of an author or genre that still has books
- **THEN** the record is not removed and the user sees a message explaining the record is in use

### Requirement: Book relationships visible

The books section SHALL display each book's author and genre by name, and the book form SHALL let the user pick author and genre from the existing catalogs rather than typing free text.

#### Scenario: Book list shows relationships

- **WHEN** user views the books list
- **THEN** each row shows the book title together with its author name and genre name

#### Scenario: Book form uses catalog pickers

- **WHEN** user creates or edits a book
- **THEN** author and genre are chosen from selectors populated from the existing catalogs, so invalid references cannot be entered

### Requirement: Loading and empty states

Every list and form view SHALL indicate when data is loading and SHALL show an explicit empty state when a catalog has no records or no search results.

#### Scenario: Empty catalog messaging

- **WHEN** user opens a section whose catalog contains no records
- **THEN** the view displays a clear empty-state message and a path to create the first record
