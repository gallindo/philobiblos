# Acceptance Criteria — Philobiblos

This document lists the acceptance criteria for every functionality delivered in the Philobiblos challenge. Criteria are written in **Given / When / Then** format and are directly derived from the OpenSpec capability specifications and the implemented behavior. They can be used for manual QA, the technical presentation, or as a regression checklist.

---

## 1. Genre Management

### 1.1 Create a genre

- **AC-1.1.1** Given a valid genre name, when a `POST /api/genres` request is made, then the response status is `201 Created`, the body contains the created genre (`id`, `name`), and the `Location` header points to `GET /api/genres/{id}`.
- **AC-1.1.2** Given a request with a missing, empty, or whitespace-only name, when submitted, then the response status is `400 Bad Request` with `application/problem+json`, and the `errors` object contains an entry for `name`.
- **AC-1.1.3** Given a request with a name longer than 100 characters, when submitted, then the response status is `400 Bad Request` with a validation error for `name`.

### 1.2 Genre name uniqueness

- **AC-1.2.1** Given a genre named "Fantasy" already exists, when a create or update request uses "fantasy" (case-insensitive) or "  Fantasy  " (after trimming), then the response status is `409 Conflict` with a clear `detail` message.

### 1.3 List and search genres

- **AC-1.3.1** Given genres exist, when `GET /api/genres` is called without filters, then the response is a paged envelope (`items`, `page`, `pageSize`, `totalCount`) containing genres ordered by the documented default sort with deterministic tie-breaking.
- **AC-1.3.2** Given genres exist, when `GET /api/genres?name=fic` is called, then only genres whose names contain "fic" case-insensitively are returned, and pagination metadata reflects the filtered total.
- **AC-1.3.3** Given genres exist, when `GET /api/genres?sort=name&direction=desc` is called, then results are sorted by name descending with stable ordering.
- **AC-1.3.4** Given an unsupported sort field (e.g. `sort=createdAt`), when requested, then the response status is `400 Bad Request` with a validation error for `sort`.

### 1.4 Get a genre by id

- **AC-1.4.1** Given an existing genre id, when `GET /api/genres/{id}` is called, then the response status is `200 OK` with the genre representation.
- **AC-1.4.2** Given a non-existent genre id, when called, then the response status is `404 Not Found` with `application/problem+json` and a `detail` message.

### 1.5 Update a genre

- **AC-1.5.1** Given an existing genre, when a valid `PUT /api/genres/{id}` request is made, then the response status is `200 OK` with the updated genre.
- **AC-1.5.2** Given a non-existent genre id, when updated, then the response status is `404 Not Found`.
- **AC-1.5.3** Given the update causes a name conflict, when submitted, then the response status is `409 Conflict`.
- **AC-1.5.4** Given an invalid name (empty or too long), when submitted, then the response status is `400 Bad Request` with the appropriate field error.

### 1.6 Delete a genre

- **AC-1.6.1** Given a genre with no associated books, when `DELETE /api/genres/{id}` is called, then the response status is `204 No Content` and the genre is removed.
- **AC-1.6.2** Given a genre with one or more associated books, when deleted, then the response status is `409 Conflict` with a `detail` message explaining the genre is in use.
- **AC-1.6.3** Given a non-existent genre id, when deleted, then the response status is `404 Not Found`.

---

## 2. Author Management

### 2.1 Create an author

- **AC-2.1.1** Given a valid name (and optional biography), when `POST /api/authors` is called, then the response status is `201 Created`, the body contains the author (`id`, `name`, `bio`), and the `Location` header points to the resource.
- **AC-2.1.2** Given a request with a missing, empty, or whitespace-only name, when submitted, then the response status is `400 Bad Request` with a `name` validation error.
- **AC-2.1.3** Given a name longer than 150 characters or a biography longer than 2000 characters, when submitted, then the response status is `400 Bad Request` with validation errors for the offending field(s).

### 2.2 Author name uniqueness

- **AC-2.2.1** Given an author named "Asimov" already exists, when a create or update request uses "asimov" or "  Asimov  ", then the response status is `409 Conflict`.

### 2.3 List and search authors

- **AC-2.3.1** Given authors exist, when `GET /api/authors` is called, then the response is a paged envelope with authors.
- **AC-2.3.2** Given authors exist, when `GET /api/authors?name=asim` is called, then only authors whose names contain "asim" case-insensitively are returned.
- **AC-2.3.3** Given authors exist, when sorted by name descending, then results are returned in that order with stable tie-breaking.
- **AC-2.3.4** Given an unsupported sort field, when requested, then the response status is `400 Bad Request`.

### 2.4 Get an author by id

- **AC-2.4.1** Given an existing author id, when `GET /api/authors/{id}` is called, then the response status is `200 OK` with the author representation.
- **AC-2.4.2** Given a non-existent author id, when called, then the response status is `404 Not Found`.

### 2.5 Update an author

- **AC-2.5.1** Given an existing author, when a valid `PUT /api/authors/{id}` request is made, then the response status is `200 OK` with the updated author.
- **AC-2.5.2** Given a non-existent author id, when updated, then the response status is `404 Not Found`.
- **AC-2.5.3** Given the update causes a name conflict, when submitted, then the response status is `409 Conflict`.
- **AC-2.5.4** Given invalid field values, when submitted, then the response status is `400 Bad Request`.

### 2.6 Delete an author

- **AC-2.6.1** Given an author with no associated books, when `DELETE /api/authors/{id}` is called, then the response status is `204 No Content`.
- **AC-2.6.2** Given an author with one or more associated books, when deleted, then the response status is `409 Conflict`.
- **AC-2.6.3** Given a non-existent author id, when deleted, then the response status is `404 Not Found`.

---

## 3. Book Management

### 3.1 Create a book

- **AC-3.1.1** Given a valid title and existing `authorId` and `genreId`, when `POST /api/books` is called, then the response status is `201 Created`, the body contains the book including the embedded author and genre summaries, and the `Location` header is present.
- **AC-3.1.2** Given a request with a missing, empty, or whitespace-only title, when submitted, then the response status is `400 Bad Request` with a `title` validation error.
- **AC-3.1.3** Given a title longer than 200 characters, when submitted, then the response status is `400 Bad Request` with a `title` validation error.
- **AC-3.1.4** Given an `authorId` or `genreId` that does not exist, when submitted, then the response status is `400 Bad Request` with the corresponding field error (`authorId` or `genreId`).

### 3.2 Book relationship integrity

- **AC-3.2.1** Given any book representation returned by the API, when inspected, then it includes the author's `id` and `name` and the genre's `id` and `name`.
- **AC-3.2.2** Given a book update, when the `authorId` or `genreId` is changed, then the response reflects the new relationships.

### 3.3 ISBN validation and uniqueness

- **AC-3.3.1** Given a request with a valid ISBN-10 or ISBN-13 (with or without hyphens/spaces), when submitted, then the book is accepted and the ISBN is stored normalized.
- **AC-3.3.2** Given a request with an invalid ISBN format or checksum, when submitted, then the response status is `400 Bad Request` with an `isbn` validation error.
- **AC-3.3.3** Given another book already has the same ISBN, when submitted, then the response status is `409 Conflict`.
- **AC-3.3.4** Given the ISBN is omitted, when submitted, then the book is accepted with `isbn: null`.

### 3.4 Publication year sanity

- **AC-3.4.1** Given a `publishedYear` between 1450 and the current year inclusive, when submitted, then the book is accepted.
- **AC-3.4.2** Given a `publishedYear` in the future, when submitted, then the response status is `400 Bad Request` with a `publishedYear` validation error.

### 3.5 List, search, and sort books

- **AC-3.5.1** Given books exist, when `GET /api/books` is called, then the response is a paged envelope of books including author and genre names.
- **AC-3.5.2** Given books exist, when `GET /api/books?title=rama` is called, then only books whose titles contain "rama" case-insensitively are returned.
- **AC-3.5.3** Given books exist, when `GET /api/books?authorId={id}` is called, then only books by that author are returned.
- **AC-3.5.4** Given books exist, when `GET /api/books?genreId={id}` is called, then only books in that genre are returned.
- **AC-3.5.5** Given books exist, when combined filters are used (e.g. `title=rama&genreId={id}`), then only books matching all criteria are returned.
- **AC-3.5.6** Given books exist, when sorted by `title` or `publishedYear` (ascending or descending), then results are returned in the requested order.
- **AC-3.5.7** Given an unsupported sort field, when requested, then the response status is `400 Bad Request`.

### 3.6 Get a book by id

- **AC-3.6.1** Given an existing book id, when `GET /api/books/{id}` is called, then the response status is `200 OK` with the book including author and genre.
- **AC-3.6.2** Given a non-existent book id, when called, then the response status is `404 Not Found`.

### 3.7 Update a book

- **AC-3.7.1** Given an existing book, when a valid `PUT /api/books/{id}` request is made, then the response status is `200 OK` with the updated book including new relationships.
- **AC-3.7.2** Given a non-existent book id, when updated, then the response status is `404 Not Found`.
- **AC-3.7.3** Given invalid field values, missing references, or a duplicate ISBN, when submitted, then the response status is `400` or `409` with the appropriate error details.

### 3.8 Delete a book

- **AC-3.8.1** Given an existing book, when `DELETE /api/books/{id}` is called, then the response status is `204 No Content`, and the related author and genre remain intact.
- **AC-3.8.2** Given a non-existent book id, when deleted, then the response status is `404 Not Found`.

---

## 4. API Contract (Cross-Cutting)

### 4.1 Uniform error responses

- **AC-4.1.1** Given any error response (4xx or 5xx), when inspected, then the `Content-Type` is `application/problem+json` and the body contains `type`, `title`, `status`, and `detail`.
- **AC-4.1.2** Given any error response, when inspected, then it does not contain stack traces, connection strings, or internal class names.

### 4.2 Validation errors

- **AC-4.2.1** Given a validation failure, when the response is returned, then it has status `400 Bad Request` and an `errors` object mapping each failing field to an array of messages.
- **AC-4.2.2** Given multiple validation failures, when returned, then all failing fields are reported together.

### 4.3 Status code semantics

- **AC-4.3.1** Given a successful create, when the response is returned, then the status is `201 Created` with a `Location` header.
- **AC-4.3.2** Given a successful read or update, when returned, then the status is `200 OK`.
- **AC-4.3.3** Given a successful delete, when returned, then the status is `204 No Content`.
- **AC-4.3.4** Given a missing resource, when requested, then the status is `404 Not Found`.
- **AC-4.3.5** Given a conflict (duplicate name/ISBN, delete-in-use), when returned, then the status is `409 Conflict`.

### 4.4 Global exception handling

- **AC-4.4.1** Given an unhandled exception occurs on the server, when the response is returned, then the status is `500 Internal Server Error`, the body is a ProblemDetails response, and it includes a `correlationId`.
- **AC-4.4.2** Given a `500` response, when server logs are inspected, then a log entry with the same `correlationId` is present.

### 4.5 Pagination envelope

- **AC-4.5.1** Given any list endpoint, when called, then the response contains `items`, `page`, `pageSize`, and `totalCount`.
- **AC-4.5.2** Given `page` is less than 1, `pageSize` is less than 1, or `pageSize` is greater than 100, when requested, then the response status is `400 Bad Request`.
- **AC-4.5.3** Given a valid paged request, when the response is returned, then `totalCount` reflects the full result set before paging, and `items` contains only the requested page.

### 4.6 Sort whitelist

- **AC-4.6.1** Given a supported sort field for the endpoint, when requested, then results are sorted accordingly.
- **AC-4.6.2** Given no sort parameter, when requested, then results use the endpoint's documented deterministic default order.
- **AC-4.6.3** Given an unsupported sort field, when requested, then the response status is `400 Bad Request`.

---

## 5. Single-Page Application (Library SPA)

### 5.1 Navigation

- **AC-5.1.1** Given the user is on any page, when the navigation links (Genres, Authors, Books) are clicked, then the corresponding management view is displayed without a full page reload.
- **AC-5.1.2** Given the SPA is loaded at `http://localhost:4200`, then the default view is one of the management sections (e.g. `/genres`).

### 5.2 List views

- **AC-5.2.1** Given a management section, when opened, then it displays a loading indicator while fetching.
- **AC-5.2.2** Given records exist, when the section loads, then they are displayed in a table with pagination controls.
- **AC-5.2.3** Given no records exist, when the section loads, then an empty-state message is shown with a path to create the first record.
- **AC-5.2.4** Given the user types in the search field, when submitted or debounced, then the list refreshes to matching records and resets to page 1.
- **AC-5.2.5** Given the user navigates pages, when a new page is selected, then the list updates and shows the current page and total count.

### 5.3 Create and edit records

- **AC-5.3.1** Given the user clicks to create a new record, when the form is displayed, then required fields are visually indicated.
- **AC-5.3.2** Given the user submits invalid data, when the server responds with `400`, then field errors are displayed next to the corresponding controls without losing input.
- **AC-5.3.3** Given the user submits data that causes a `409` conflict, when the response arrives, then a non-field error message is displayed.
- **AC-5.3.4** Given the user edits an existing record, when saved, then the list refreshes and reflects the change.

### 5.4 Delete records

- **AC-5.4.1** Given the user clicks delete on a record with no blocking relationships, when they confirm, then the record disappears and a success indication is shown.
- **AC-5.4.2** Given the user clicks delete on an author or genre with associated books, when they confirm, then the record remains and an in-use message is shown.

### 5.5 Book relationships

- **AC-5.5.1** Given the books list, when displayed, then each row shows the book title together with the author name and genre name.
- **AC-5.5.2** Given the book create/edit form, when displayed, then author and genre are chosen from `<select>` dropdowns populated from the existing catalogs.
- **AC-5.5.3** Given the book form, when submitted, then only valid author/genre identifiers can be sent to the API.

### 5.6 Error handling

- **AC-5.6.1** Given the API is unreachable or returns `500`, when the SPA handles the error, then a user-readable error message is displayed.
- **AC-5.6.2** Given the backend returns validation errors, when the form is displayed, then each field shows its server-provided message.

---

## 6. Local Execution & Containerization

- **AC-6.1** Given Docker and Docker Compose are installed, when `docker compose up --build` is run from the repository root, then the database, API, and web services start and the SPA is accessible at `http://localhost:4200`.
- **AC-6.2** Given the services are running, when the API is accessed at `http://localhost:8080`, then migrations have been applied and endpoints respond correctly.
- **AC-6.3** Given the services are running, when `docker compose down -v` is executed, then all containers and volumes are removed.
- **AC-6.4** Given the .NET SDK and a local PostgreSQL instance, when `dotnet run --project src/Philobiblos.Api/Philobiblos.Api.csproj` is run from `backend/`, then the API starts and auto-applies migrations in Development.
- **AC-6.5** Given Node.js is installed, when `npm run start` is run from `frontend/`, then `ng serve` starts on `http://localhost:4200` and proxies `/api` requests to `http://localhost:8080`.

---

## 7. Automated Tests

- **AC-7.1** Given the backend solution, when `dotnet test` is run, then all unit tests pass (92 tests covering validators and business-rule branches).
- **AC-7.2** Given Docker is available, when `dotnet test` is run, then all integration tests pass (40 tests covering CRUD and error-contract scenarios against a real PostgreSQL container).
- **AC-7.3** Given the frontend workspace, when `npm run build` is run, then the production build succeeds with no errors.

---

## 8. Documentation

- **AC-8.1** Given the repository root, when `README.md` is read, then it contains: solution overview, prerequisites, quick start, architecture, backend organization, frontend organization, database choice, main trade-offs, testing strategy, known limitations, and improvements with more time.
- **AC-8.2** Given `docs/adr/`, when the files are read, then ADRs 0001–0004 exist and each contains context, decision, and consequences for the major architectural choices.
