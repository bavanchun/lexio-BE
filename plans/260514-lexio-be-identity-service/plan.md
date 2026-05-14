---
title: "Lexio Identity microservice (vertical slice on BE foundation)"
description: "First Lexio microservice: OpenIddict 6 + EF Core 10 + Postgres + bcrypt + RS256 JWT + RBAC + Outbox/MassTransit, with FE swap from Zustand stub to httpOnly-cookie proxy."
status: pending
priority: P1
effort: 42h
branch: feat/be-identity-scaffold
tags: [backend, identity, openiddict, ef-core-10, postgres, rbac, jwt, vertical-slice, fe-swap]
created: 2026-05-14
---

# Lexio Identity microservice

First production microservice on top of completed BE foundation (PR #1–#12). Full vertical slice: Domain → Application → Infrastructure → Api + tests + migrations + MassTransit publishers + FE swap. Source of truth: `docs/Lexio_Complete_Documentation.docx` §§3.3, 6.7, 7.1, 9.1. Researcher-locked stack: OpenIddict 6 (MIT), EF Core 10.0.10 + Npgsql 10.0.4, bcrypt cost 12, RS256 JWT (15-min access / 7-day refresh), 5 RBAC roles, 11 audit events, outbox via `LexioDbContextBase`.

## Locked decisions

| Concern | Choice | Source |
|---|---|---|
| Auth server | OpenIddict 6 (MIT) | researcher-01 |
| EF Core | 10.0.10 + Npgsql 10.0.4 | researcher-02 |
| Strong-typed IDs | `readonly record struct UserId(Guid)` + value converter | researcher-02 §3 |
| Password hashing | BCrypt.Net-Next, cost 12 | researcher-04 §3.2 |
| Access token | RS256 JWT, 15 min | researcher-04 §7.1 |
| Refresh token | 7d, bcrypt-hashed at rest, rotation on issue, **30s grace window for in-flight requests** | researcher-04 §7.2 + user 2026-05-14 |
| Postgres version | **PG 18** (UUIDv7 PKs + virtual columns) | user 2026-05-14 |
| Ban enforcement | **JWT `banned` claim + 60s in-process cache** | user 2026-05-14 |
| OAuth scope | **Google OAuth included in this plan** (GitHub deferred) | user 2026-05-14 |
| Role seed mechanism | **Idempotent SQL script in migration** (`infra/db/seed/identity-roles.sql`) | user 2026-05-14 |
| FE auth backend toggle | **Runtime flag via Zustand store + localStorage** (`authBackend: 'stub' \| 'real'`) | user 2026-05-14 |
| Roles | Guest, Learner, Verified Creator, Moderator, Admin (5) | researcher-04 §1.2 |
| Domain events | UserRegistered, UserLoggedIn, PasswordChanged, RoleChanged, UserBanned (via outbox → MassTransit) | researcher-04 §6 |
| Audit log | 11 event types → Kafka topic `vocab.audit-log` | researcher-04 §5 |
| Time type | `DateTimeOffset` (BuildingBlocks consistency) | researcher-02 §4 |
| Soft-delete | `ISoftDeletableEntity` + global query filter | researcher-02 §5 |
| FE token storage | httpOnly cookie via Next.js route-handler proxy | researcher-03 §3 |

## Phase list & dependency graph

```
01 ── 02 ── 03 ── 04 ── 05 ── 06 ── 07 ── 07b ── 08 ── 09
                                                       └── 10 ── 11 ── 12
```

Phase 07b (Google OAuth) inserted post-decision; renumber on next plan refresh if preferred.

| # | Phase | File | Depends | Effort | Status |
|---|-------|------|---------|--------|--------|
| 01 | Scaffold Identity from `lexio-service` template (csproj, DI wiring, health, OTel) | `phase-01-scaffold-identity-from-template.md` | foundation | 2h | pending |
| 02 | Domain layer: `User` aggregate, value objects, `Role`, `RefreshToken`, `OAuthConnection`, domain events | `phase-02-domain-layer.md` | 01 | 4h | pending |
| 03 | Application layer: Mediator commands/queries, FluentValidation, Mapster profiles, RBAC policies | `phase-03-application-layer.md` | 02 | 4h | pending |
| 04 | Infrastructure: `IdentityDbContext`, EntityTypeConfigurations, EF Core 10 migration, outbox wiring | `phase-04-infrastructure-ef-core-postgres.md` | 03 | 3h | pending |
| 05 | OpenIddict 6 server: token endpoint, RS256 signing, refresh rotation, bcrypt password service | `phase-05-openiddict-and-password-service.md` | 04 | 4h | pending |
| 06 | Api layer: minimal API endpoints (8 per contract), Swagger, ProblemDetails, rate limiting, CORS | `phase-06-api-endpoints-and-cors.md` | 05 | 3h | pending |
| 07 | MassTransit publishers via outbox + Kafka audit-log producer | `phase-07-masstransit-outbox-and-kafka-audit.md` | 04, 06 | 3h | pending |
| 07b | Google OAuth: external login handler, `oauth_connections` write, account-link flow | `phase-07b-google-oauth.md` | 06, 07 | 4h | pending |
| 08 | Test infra: unit (xUnit v3), integration (Testcontainers Postgres), architecture (NetArchTest), contract | `phase-08-test-suite.md` | 02–07b | 4h | pending |
| 09 | docker-compose update + `.env.example` + secrets runbook for Identity | `phase-09-docker-compose-and-secrets.md` | 06 | 2h | pending |
| 10 | FE swap phase 1: route handlers + httpOnly cookie + env + CSP (8 FE files) | `phase-10-fe-swap-route-handlers-and-cookie.md` | 06, 09 | 3h | pending |
| 11 | FE swap phase 2: replace Zustand auth-store calls, error mapping, feature flag, Serwist rule | `phase-11-fe-swap-store-and-serwist.md` | 10 | 3h | pending |
| 12 | E2E Playwright (FE → BE happy path) + docs + ADRs | `phase-12-e2e-tests-and-adrs.md` | 11 | 3h | pending |

Total: ~42h (38h base + 4h Google OAuth). Each phase = 1 stacked PR, branched off the previous phase's branch (NOT main), per BE-foundation precedent (PR #1–#12).

## Branch / commit conventions

- Branch: `feat/be-identity-{kebab}` (FE phases: `feat/fe-identity-{kebab}` in lexio-app-fe repo)
- Commits: Conventional Commits, scope `be-identity` (or `fe-identity` for FE repo)
- Stacked PRs: phase-02 branches off phase-01, etc. Merge into `main` only after all 12 PRs reviewed.

## Cross-phase invariants

- No `Microsoft.AspNetCore.*` references from `Lexio.Identity.Domain` or `Lexio.Identity.Application`.
- `dotnet build /warnaserror` clean and `dotnet format --verify-no-changes` clean on every PR.
- Password never logged, never returned in API responses, never present in DTOs after hash.
- Refresh token stored only as bcrypt hash; never logged.
- All domain events go through outbox (`LexioDbContextBase.CollectOutboxMessages`) — never directly to MassTransit bus.
- All entity datetime columns: `TIMESTAMP WITH TIME ZONE`, `DateTimeOffset` in CLR.
- snake_case naming convention on Postgres.
- ProblemDetails RFC 7807 on every non-2xx response.

## Success criteria

1. `dotnet test` green: ≥80% line coverage on Domain + Application, integration tests pass against Testcontainers Postgres.
2. `curl POST /api/v1/auth/register → 201` with valid access+refresh tokens; `GET /api/v1/auth/me → 200` with bearer.
3. `outbox_messages` row created in same transaction as User insert; MassTransit publishes `UserRegisteredEvent` to RabbitMQ; visible on consumer side.
4. FE login form (lexio-app-fe) authenticates against real BE, sets httpOnly cookie, hydrates Zustand store, navigates to `/decks`.
5. Playwright E2E: register → login → me → logout flow green.
6. ADRs landed: `adr-013-identity-openiddict.md`, `adr-014-refresh-token-rotation.md`, `adr-015-fe-cookie-proxy.md`.

## Unresolved questions

All 6 prior open questions resolved by user on 2026-05-14 (see "Locked decisions" table). Decisions propagated to phase files on 2026-05-14:
- `phase-04-infrastructure-ef-core-postgres.md` — PG 18 + `uuidv7()` PK defaults; `infra/db/seed/identity-roles.sql` idempotent UPSERT seed; `SetPostgresVersion(18,0)`; virtual generated column noted as out-of-scope.
- `phase-05-openiddict-and-password-service.md` — 30s refresh-token grace window (`revoked_at = now + 30s` semantics); `RefreshToken.IsActive(IClock)` predicate; unit test for grace window; ADR-014 trade-off note.
- `phase-06-api-endpoints-and-cors.md` — `banned` JWT claim + `BannedUserAuthorizationHandler` + `NotBanned` policy; 60s `IMemoryCache` ban cache on write endpoints; cache invalidation on admin ban/unban.
- `phase-07b-google-oauth.md` (NEW) — Google OAuth start/callback, `LinkOrCreateFromExternalCommand`, `OAuthConnectedEvent` (domain + contracts), WireMock.Net integration tests, security envelope (state/nonce/PKCE, returnUrl allowlist).
- `phase-11-fe-swap-store-and-serwist.md` — `useAuthConfigStore` Zustand+persist replaces `NEXT_PUBLIC_AUTH_BACKEND`; `getAuthClient()` call-site branching; dev-only `auth-debug-panel.tsx`.

No remaining open questions.
