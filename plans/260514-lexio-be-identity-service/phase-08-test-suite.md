# Phase 08 — Test suite (unit, integration, architecture, contract)

## Context Links
- Foundation phase-09 — Testcontainers, NetArchTest, TestUtils, `.runsettings` already in place
- researcher-04 §1–§6 — full contract used as oracle for contract tests
- All preceding phases (02–07) — units under test

## Overview
- Priority: P1
- Status: pending
- Effort: 4h
- Branch: `feat/be-identity-tests` (off phase-07)
- PR: stacked PR #20

Fill gaps left by per-phase tests; add cross-cutting suites. Targets: ≥85% line coverage on Domain + Application, ≥70% on Infrastructure + Api. NetArchTest enforces clean-arch layering. Contract tests use researcher-04 as the spec.

## Key Insights
- Per-phase tests already cover: Domain (phase-02), Application handlers (phase-03), bcrypt + token (phase-05), endpoint smoke (phase-06), messaging integration (phase-07). This phase **fills gaps** + **adds architecture + contract** suites.
- Testcontainers shared `IAsyncLifetime` fixture spins up Postgres + RabbitMQ + Kafka once per test class collection; reduces test wall-clock.
- Contract tests: derive expected status codes + ProblemDetails URIs from researcher-04 §1 + §4 tables; fail if endpoint returns different shape.
- Coverage gate: `coverlet.collector` + `dotnet test --collect:"XPlat Code Coverage"` + threshold via `Directory.Build.targets` (foundation already wires).

## Requirements
**Functional**
- Unit suite (`*.Tests`) per project — already scaffolded in phases 02–05. This phase adds missing branches (error mapping, edge cases).
- Integration suite under `tests/Lexio.Identity.IntegrationTests/` (NEW project):
  - `IdentityWebApplicationFactory` builds full Api with real DbContext bound to Testcontainers Postgres + Testcontainers RabbitMQ + Testcontainers Kafka.
  - Test cases: full register → login → me → refresh → logout flow; concurrent refresh rotation (2 parallel rotate calls → exactly one succeeds); login rate-limit triggers after 5 attempts.
- Architecture suite under `tests/Lexio.Identity.ArchitectureTests/` (NEW project):
  - Domain references nothing other than SharedKernel + BuildingBlocks.Abstractions.
  - Application references nothing other than Domain + BuildingBlocks.Abstractions + FluentValidation + Mapster + Mediator.
  - Infrastructure may reference EF + OpenIddict + MassTransit + Domain + Application + Contracts.
  - Api references Application + Infrastructure + Contracts only.
- Contract suite under `tests/Lexio.Identity.ContractTests/` (NEW project):
  - Parametrised test runs every endpoint × every documented status code, asserting ProblemDetails `type` + `status` match the spec table.

**Non-functional**
- All test projects use `Microsoft.NET.Test.Sdk` + `xunit.v3` + `Moq` + `FluentAssertions` (foundation-pinned).
- Total suite runtime < 90s on dev hardware.
- `dotnet test` from CI green; coverage report uploaded as artifact.

## Architecture
```
tests/
├── Lexio.Identity.Domain.Tests/             (exists, expanded)
├── Lexio.Identity.Application.Tests/        (exists, expanded)
├── Lexio.Identity.Infrastructure.Tests/     (exists, expanded)
├── Lexio.Identity.Api.Tests/                (exists, expanded)
├── Lexio.Identity.IntegrationTests/         (NEW — full stack)
├── Lexio.Identity.ArchitectureTests/        (NEW — NetArchTest)
└── Lexio.Identity.ContractTests/            (NEW — researcher-04 oracle)
```

## Related Code Files
**Create:** 3 new test projects + fixtures + ~30 test classes.
**Modify:** `Lexio.slnx` adds new test projects; `.github/workflows/ci.yml` adds coverage threshold step (≥80% total).
**Delete:** none.

## Implementation Steps
1. Expand Domain.Tests: add property-based test (FsCheck or AutoFixture) for Email canonicalisation; assert `Email.Create("A@B.COM")` equals `Email.Create("a@b.com")`.
2. Expand Application.Tests:
   - `LoginCommandHandler` table-driven: (no-user, wrong-pass, banned, success) — all 4 cases verify constant-time bcrypt verify path.
   - `RefreshTokenCommandHandler` — rotation; reuse-of-old-token rejection.
3. Expand Infrastructure.Tests: round-trip `User` save/load via Testcontainers; verify soft-delete query filter excludes deleted rows.
4. Create `IdentityWebApplicationFactory : WebApplicationFactory<Program>`:
   - Overrides `ConfigureWebHost` to swap connection string to Testcontainers Postgres.
   - Runs migrations on startup.
   - Resets DB between tests via `Respawn` library.
5. IntegrationTests classes:
   - `RegisterLoginFlowTests` — happy path + uniqueness + weak password.
   - `RefreshRotationTests` — concurrent rotation, replay attack rejection.
   - `RateLimitTests` — 6 logins → 429.
   - `OutboxToRabbitMqTests` — register → consume `UserRegisteredEvent` from RMQ.
   - `AuditLogTests` — register → `audit_logs` row + Kafka consume.
6. ArchitectureTests:
   ```csharp
   Types.InAssembly(typeof(User).Assembly)
     .Should().NotHaveDependencyOn("Microsoft.AspNetCore")
     .Should().NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
     .GetResult().IsSuccessful.Should().BeTrue();
   ```
   Apply per-layer rules.
7. ContractTests: data-driven XUnit `[Theory]` from a `static IEnumerable<object[]>` derived from researcher-04 §1 + §4 (encoded as a JSON fixture committed under `tests/Lexio.Identity.ContractTests/spec/identity-contract.json`).
8. Wire coverage threshold in `Directory.Build.targets` for test projects.
9. Run full `dotnet test`; ensure < 90s + green.
10. PR #20 stacked on phase-07.

## Todo List
- [ ] Expanded Domain.Tests + Application.Tests + Infrastructure.Tests
- [ ] IntegrationTests project + Testcontainers fixtures
- [ ] ArchitectureTests project
- [ ] ContractTests project + spec fixture
- [ ] Coverage threshold ≥80% enforced in CI
- [ ] Total suite < 90s
- [ ] PR #20 opened

## Success Criteria
- `dotnet test Lexio.slnx` — all green.
- Coverage report: Domain ≥90%, Application ≥85%, Infrastructure ≥75%, Api ≥70%.
- ArchitectureTests fail-fast on any boundary violation introduced later.
- ContractTests fail if researcher-04 spec drifts from implementation.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Testcontainers slow on CI runners | H | M | Cache Docker layers in GitHub Actions; use small Alpine images; run integration suite only on `main` branch PRs not feature branches. |
| Flaky concurrent rotation test | M | M | Use `Task.WhenAll(2 parallel rotates)`; assert exactly one ok + one 401, retry up to 3× on infrastructure flakes. |
| Contract spec drift hidden by overly-loose ProblemDetails matchers | M | H | Match exact `type` URI + `status` + presence of `instance`. |
| Respawn deletes OpenIddict tables breaking auth across tests | M | M | Configure Respawn to preserve `__ef_migrations_history` + OpenIddict scope rows; only purge runtime data. |

## Security Considerations
- Test JWT signing key is the dev key (in-memory) — never reused in prod.
- Test passwords use deterministic plaintexts ("TestPass!1") — never commit production-shaped credentials.
- DB credentials in test config = `postgres/postgres` (container-only).

## Next Steps
Unblocks phase-09 (CI gates green) and phase-12 (E2E builds on top of integration suite patterns).
