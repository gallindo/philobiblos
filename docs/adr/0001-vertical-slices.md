# ADR 0001: Vertical Slice Architecture over Clean Architecture layers

## Context

Philobiblos has three entities (`Genre`, `Author`, `Book`) and mostly pure-CRUD use cases. A classic Clean Architecture split would create near-empty `Domain`/`Application` projects and force every new field to be edited in four or five places (entity, DTO, validator, handler, repository interface).

## Decision

Organize the API by feature rather than by technical layer. Each slice file in `backend/src/Philobiblos.Api/Features/<Entity>/` owns its route registration, request/response DTOs, validator, and handler. Shared concerns (EF configuration, middleware, paging) live in `Data/` and `Infrastructure/`.

## Consequences

- **Positive:** A use case changes in one file. Less ceremony, high cohesion, and fast onboarding.
- **Positive:** The structure scales naturally to more entities and more complex commands.
- **Risk:** Reviewers expecting layered Clean Architecture may misread slices as unstructured. The README and this ADR frame the choice as deliberate right-sizing and name Clean Architecture as the evolution path if aggregates or domain events appear.
