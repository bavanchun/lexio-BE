# 0002. Clean Architecture per Service

Date: 2026-05-10

## Status

Accepted

## Context

Each microservice needs a consistent internal layer structure to:
- Enforce separation between domain logic and infrastructure concerns.
- Allow infrastructure to be swapped (e.g., switching DB provider) without touching domain.
- Enable architecture tests to verify rules automatically.

Doc §6.7 mandates a layered approach. The two main options are:
- **Vertical slices** (feature folders cut across layers) — good for small teams, less boilerplate.
- **Clean Architecture** (horizontal layers: Domain → Application → Infrastructure → Api) — stricter isolation.

## Decision

Each service uses four layers with strict dependency rules enforced by NetArchTest:

```
Domain          — entities, value objects, domain events, domain services, repository interfaces
  ↑
Application     — use cases (commands/queries), handlers, validators, DTOs, mapping configs
  ↑
Infrastructure  — EF Core DbContext, repository implementations, external adapters
  ↑
Api             — controllers, minimal API endpoints, middleware, DI wiring
```

Dependency rule: outer layers reference inner layers; inner layers never reference outer.

Violations caught at build time by `Lexio.Architecture.Tests` (NetArchTest) and the per-service
architecture test in `tests/Lexio.{Service}.Api.Tests/ArchitectureTests.cs`.

## Consequences

**Positive:**
- Domain logic is framework-agnostic and independently unit-testable.
- Architecture violations surface as CI failures, not code review findings.
- New developers follow a predictable structure across all services.

**Negative:**
- Higher boilerplate for simple CRUD services; mitigated by `Lexio.ServiceTemplate`.
- Mapping between layers (domain ↔ DTO) adds code; Mapster minimises ceremony.

**Neutral:**
- `Lexio.SharedKernel` provides `AggregateRoot<TId>`, `Entity<TId>`, `ValueObject`, `Result<T>`,
  `Maybe<T>`, `Error` — consumed by the Domain layer of every service.
