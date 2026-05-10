# 0001. Monorepo with .NET 10

Date: 2026-05-10

## Status

Accepted

## Context

Lexio BE consists of multiple microservices (Identity, Vocabulary, Progress, Notification, …).
Each service shares DDD primitives, cross-cutting building blocks, and CI tooling.
We must choose: one repository per service (multi-repo) or all services in one repository (monorepo).

Key constraints:
- Small team (1–3 engineers initially); low coordination overhead matters.
- Shared `Lexio.SharedKernel` and `Lexio.BuildingBlocks.*` assemblies must stay in sync across services.
- CI must enforce architecture invariants across all services in a single pass.

## Decision

Use a single git repository (`lexio-BE`) containing all backend services and shared libraries,
managed by a single `Lexio.slnx` solution file and Central Package Management (`Directory.Packages.props`).

Target SDK: **.NET 10** (current LTS track as of project inception).

## Consequences

**Positive:**
- Atomic refactors across service + shared library boundaries in one PR.
- Single CI pipeline; architecture tests scan all assemblies.
- Dependency versions centrally managed — no version skew between services.

**Negative:**
- Repository grows large over time; requires disciplined module boundaries.
- Longer CI times as service count grows (mitigated with job parallelism and caching).

**Neutral:**
- Service teams work on feature branches; merges to `main` require PR review.
- `scripts/new-service.sh` scaffold keeps per-service setup fast.
