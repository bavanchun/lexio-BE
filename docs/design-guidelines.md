# Design Guidelines

> **Canonical source:** `docs/Lexio_Complete_Documentation.docx` §6 (API design), §12 (brand).
> This file covers BE API design conventions only. FE design lives in `lexio-FE/docs/`.

## REST API Conventions

- Base path: `/api/v{n}/{service}/{resource}`
- Versioning: URL segment (`/api/v1/`, `/api/v2/`) — no header-based versioning.
- Casing: `camelCase` for JSON request/response fields.
- Pagination: `{ items: [], total: int, page: int, pageSize: int }`.
- Error response:
  ```json
  { "code": "VALIDATION_ERROR", "message": "...", "errors": [] }
  ```

## HTTP Status Codes

| Scenario | Code |
|----------|------|
| Created | 201 + `Location` header |
| Validation failure | 400 |
| Unauthenticated | 401 |
| Forbidden | 403 |
| Not found | 404 |
| Conflict | 409 |
| Unhandled server error | 500 |

## Domain Model Design

- Prefer value objects for identity (e.g., `CardId`, `UserId` as typed wrappers).
- Use `Result<T>` / `Error` for all domain operations — never throw for expected failures.
- Aggregate roots expose only `internal` mutation methods; use factory methods for creation.
- Domain events raised via `AggregateRoot.Raise(IDomainEvent)`.

## OpenAPI

Each service's Api project exposes `/openapi/v1.json` via `Microsoft.AspNetCore.OpenApi`.
Aggregated gateway spec planned for phase 3 (YARP + OpenAPI merge).
