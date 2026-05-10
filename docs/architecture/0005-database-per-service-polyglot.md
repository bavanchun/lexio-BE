# 0005. Database per Service (Polyglot Persistence)

Date: 2026-05-10

## Status

Accepted

## Context

Microservice architecture requires each service to own its data store to avoid tight coupling.
Lexio's data has varied shapes and access patterns:

| Data type | Access pattern | Best fit |
|-----------|---------------|----------|
| User identity, progress records | relational, ACID joins | PostgreSQL |
| Flashcard content (rich, nested) | document, flexible schema | MongoDB |
| Session cache, rate limiting | TTL key-value, sub-ms | Redis |
| Full-text card search | inverted index, relevance scoring | Elasticsearch |

A single relational database would force awkward schema compromises and tight service coupling
(shared DB anti-pattern).

## Decision

Each service owns exactly one primary data store appropriate to its domain (doc §7.1):

| Service | Primary DB | Notes |
|---------|-----------|-------|
| Identity | PostgreSQL 17 | Users, sessions, OpenIddict tokens |
| Vocabulary | MongoDB 8 | Card content, decks |
| Progress | PostgreSQL 17 | SM-2 review history, schedules |
| Search | Elasticsearch 8 | Card search index (projected from Vocabulary events) |
| All services | Redis 7 | Distributed cache, session tokens |

Cross-service data needs are satisfied via **integration events**, not shared tables.
Read models are projected into the querying service's own store (CQRS read side).

EF Core 9 with Npgsql provider is used for PostgreSQL services.
MongoDB.Driver 3.x for document services.
StackExchange.Redis for cache.

## Consequences

**Positive:**
- Services deployable and scalable independently.
- Each store optimised for its access pattern.
- No cross-service DB migrations or shared schema changes.

**Negative:**
- No cross-service ACID transactions; eventual consistency accepted.
- Higher operational surface area (5 data stores to run locally + in production).
- `docker-compose.yml` starts all stores; developer machine needs ≥8 GB RAM.

**Neutral:**
- `Lexio.BuildingBlocks.Persistence` provides `LexioDbContextBase` for EF Core services.
- Per-service `IRepository<T>` implementations live in the Infrastructure layer.
- Migrations managed per-service via `dotnet-ef` tool.
