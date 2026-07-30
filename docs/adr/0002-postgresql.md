# ADR 0002: PostgreSQL as the relational database

## Context

The challenge allows SQL Server, PostgreSQL, or MySQL. The reviewer must be able to run the entire stack with one `docker compose up`, so container startup time, image size, and licensing matter.

## Decision

Use PostgreSQL 16 via the official `postgres:16-alpine` image and the Npgsql EF Core provider.

## Consequences

- **Positive:** The PostgreSQL image is ~80 MB and starts in seconds, versus ~1.5 GB for SQL Server.
- **Positive:** Npgsql is a first-class EF Core provider with strong JSONB/array support if the domain grows.
- **Positive:** No licensing friction.
- **Consequence:** Case-insensitive uniqueness is implemented with function-based indexes (`lower("Name")`) created in raw SQL inside the migration, because EF Core cannot model expression indexes directly. A filtered unique index on `Books.Isbn` handles optional ISBN uniqueness.
