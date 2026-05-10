---
title: "Lexio BE foundation bootstrap (.NET 10 monorepo, no business logic)"
description: "Scaffolding-only foundation for 7 future microservices: shared kernel, building blocks, polyglot infra, dotnet-new template, CI/CD, code quality."
status: completed
priority: P1
effort: 28h
branch: main
tags: [backend, foundation, dotnet10, clean-architecture, monorepo, scaffolding]
created: 2026-05-10
---

# Lexio BE foundation bootstrap

Foundation/scaffolding for the Lexio BE monorepo. NO microservice business logic. All decisions per source-of-truth `docs/Lexio_Complete_Documentation.docx` §§3.3, 6.7, 7.1, 9.1, 12.10 with locked overrides (.NET 10 vs doc's .NET 9; OSS Mediator vs commercial MediatR; OSS-pinned MassTransit 8.x).

## Locked stack snapshot

| Concern | Choice |
|---|---|
| Runtime | .NET 10.0.203 SDK at `$HOME/.dotnet` |
| Repo | Monorepo, single `Lexio.sln`, 7 service slots + shared/ + tests/ + infra/ |
| Architecture | Clean Architecture per service: Domain → Application → Infrastructure → Api |
| CQRS bus | `Mediator` (martinothamar) — MIT, source-gen (NOT MediatR commercial) |
| Messaging | MassTransit 8.x (last MIT) for RabbitMQ + `Confluent.Kafka` for streaming |
| Persistence | EF Core 9.x preview-on-net10 OR 10.x once GA (Postgres, Mongo for cards) |
| Validation | FluentValidation 11.x |
| Mapping | Mapster |
| Auth | JwtBearer + OpenIddict (placeholders only) |
| Observability | OpenTelemetry SDK + Serilog |
| Testing | xUnit v3 + Moq + FluentAssertions + Testcontainers + NetArchTest |
| Code quality | dotnet format + Roslyn + SonarAnalyzer.CSharp + Husky.Net + commitlint |

## Phase list & dependency graph

```
01 ── 02 ── 03 ── 04 ── 05
                  └──── 06 ── 07
                              └── 08 ── 09 ── 10 ── 11 ── 12
```

| # | Phase | File | Depends | Effort | Status |
|---|-------|------|---------|--------|--------|
| 01 | Repo init: gitignore, gitattributes, editorconfig | `phase-01-repo-init-gitignore-editorconfig.md` | — | 1h | ✅ completed (PR #1) |
| 02 | Directory.Build.props + Directory.Packages.props + analyzers + sln | `phase-02-directory-build-and-packages-props.md` | 01 | 2h | ✅ completed (PR #2) |
| 03 | `Lexio.SharedKernel` + tests | `phase-03-shared-kernel.md` | 02 | 3h | ✅ completed (PR #3) |
| 04 | `Lexio.BuildingBlocks.Abstractions` interfaces | `phase-04-building-blocks-abstractions.md` | 03 | 2h | ✅ completed (PR #4) |
| 05 | BuildingBlocks impls (Caching/Messaging/Observability/Persistence/Web/Auth) | `phase-05-building-blocks-implementations.md` | 04 | 5h | ✅ completed (PR #5) |
| 06 | `docker-compose.yml` polyglot stack + init scripts | `phase-06-docker-compose-polyglot-stack.md` | 04 | 2h | ✅ completed (PR #6) |
| 07 | Secrets & configuration strategy + `.env.example` + runbook | `phase-07-secrets-and-config-strategy.md` | 06 | 1h | ✅ completed (PR #7) |
| 08 | Custom `dotnet new lexio-service` template | `phase-08-service-template.md` | 05, 07 | 4h | ✅ completed (PR #8) |
| 09 | Test infra: Testcontainers, NetArchTest, TestUtils, .runsettings | `phase-09-testing-infrastructure.md` | 08 | 2h | ✅ completed (PR #9) |
| 10 | Husky.Net pre-commit + commit-msg hooks | `phase-10-husky-pre-commit-hooks.md` | 09 | 1h | ✅ completed (PR #10) |
| 11 | GitHub Actions CI + dependabot + cd.yml stub | `phase-11-ci-cd-skeleton.md` | 10 | 3h | ✅ completed (PR #11) |
| 12 | README + ADR set + runbooks | `phase-12-docs-skeleton-and-adrs.md` | 11 | 2h | ✅ completed (PR #12) |

Total: ~28h. Each phase = 1 stacked PR, branched off the previous phase's branch (NOT main).

## Branch / commit conventions

- Branch: `feat/be-{kebab-summary}`, `chore/be-{...}`, `docs/be-{...}`
- Commit: Conventional Commits, scope `be-{area}` — e.g. `feat(be-shared): add Result<T> primitive`
- Phase 01 branch: `feat/be-foundation-init`. Each later phase rebases on top.

## Cross-phase invariants

- All `.cs` files: `Nullable=enable`, `LangVersion=latest`, `TreatWarningsAsErrors=true`.
- All projects: SDK-style csproj, no Version on `<PackageReference>` (CPM).
- No project references `Microsoft.AspNetCore.*` from Domain/Application/SharedKernel/BuildingBlocks.Abstractions.
- Every PR must `dotnet build /warnaserror` clean and `dotnet format --verify-no-changes` clean.
- Plans + research reports tracked in git from phase 01.

## Unresolved questions

1. **Messaging library lock-in**: MassTransit 8.x pinned vs Wolverine vs raw `RabbitMQ.Client` + `Confluent.Kafka`. Default is MassTransit 8.x; revisit before phase 05 implementation.
2. **Strong-typed IDs**: hand-rolled `record struct` per id vs `Vogen` source-generator. Default hand-rolled in phase 03; revisit if boilerplate explodes.
3. **EF Core version on net10.0**: EF 9 ships net8/net9 TFMs; net10 either EF 10 (if GA by phase 05) or EF 9 with `<TargetFramework>net10.0` + reference. Confirm during phase 02 package version pinning.
4. **Mongo replica-set-of-one in dev**: deferred to standalone for v0; revisit if any service needs Mongo transactions.
5. **commitlint runner**: Node-based `@commitlint/cli` (adds Node dep) vs pure-.NET commit-msg validator (custom). Default Node-based for compatibility with FE precedent.
6. **gRPC scaffolding in template**: included always vs conditional `--grpc` flag. Default omit in v0 template; add in v0.2 once first service needs it.
7. **YARP API gateway**: out of scope for this bootstrap (separate later phase pack).
