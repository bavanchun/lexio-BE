# Phase 01 — Scaffold Identity service from `lexio-service` template

## Context Links
- Template: `templates/Lexio.ServiceTemplate/`
- Foundation phase-08: `plans/260510-1409-lexio-be-foundation-bootstrap/phase-08-service-template.md`
- researcher-02 §7 (project layout)
- Solution file: `Lexio.slnx`

## Overview
- Priority: P1
- Status: pending
- Effort: 2h
- Branch: `feat/be-identity-scaffold` (off `main`)
- PR: stacked PR #13

Scaffold four service projects (Domain, Application, Infrastructure, Api) + four test projects under `src/services/` and `tests/` using the installed `dotnet new lexio-service` template. No business logic; only project skeleton, DI placeholder methods, health endpoint, OTel wiring, build clean.

## Key Insights
- Template parameter `--name Lexio.Identity` produces correct namespaces.
- `Lexio.slnx` must be updated to include the 8 new projects (4 src + 4 tests).
- Health endpoint goes on `/healthz` (readiness) and `/livez` (liveness) per BuildingBlocks.Web convention.
- OTel auto-wires via `AddLexioObservability(...)` from BuildingBlocks.Observability.

## Requirements
**Functional**
- `dotnet build` clean on all 8 new projects.
- `dotnet run --project src/services/Lexio.Identity.Api` starts on port 5001 and `/healthz` returns 200.
- OTel exports traces to local OTLP endpoint (already running via docker-compose).

**Non-functional**
- TreatWarningsAsErrors clean.
- Solution file regenerated deterministically.

## Architecture
```
src/services/
├── Lexio.Identity.Api/          (ASP.NET Core minimal API, Program.cs)
├── Lexio.Identity.Application/  (commands/queries, no infra deps)
├── Lexio.Identity.Domain/       (entities, value objects, events)
└── Lexio.Identity.Infrastructure/ (EF, MassTransit, OpenIddict)
tests/
├── Lexio.Identity.Api.Tests/
├── Lexio.Identity.Application.Tests/
├── Lexio.Identity.Domain.Tests/
└── Lexio.Identity.Infrastructure.Tests/
```

Reference-flow: Api → Application → Domain. Infrastructure → Application + Domain. Domain references only SharedKernel + BuildingBlocks.Abstractions.

## Related Code Files
**Create:** 8 csproj files + Program.cs + DependencyInjection.cs in each tier + `appsettings.Development.json`.
**Modify:** `Lexio.slnx` (add 8 projects), `docker-compose.yml` placeholder for `identity-api` service (port 5001 reservation only — full config in phase-09).
**Delete:** none.

## Implementation Steps
1. From repo root: `dotnet new lexio-service --name Lexio.Identity --output src/services/`
2. Verify template produced expected folders; adjust paths if template emits to wrong place.
3. Add test projects: `dotnet new lexio-service-tests --name Lexio.Identity --output tests/` (or copy from template's `tests/` subfolder).
4. Update `Lexio.slnx`: `dotnet sln Lexio.slnx add src/services/Lexio.Identity.*/*.csproj tests/Lexio.Identity.*/*.csproj`.
5. In `Lexio.Identity.Api/Program.cs`: wire `AddLexioObservability`, `AddLexioWeb`, `MapHealthChecks("/healthz")`, placeholder `MapGet("/", () => "Lexio.Identity")`.
6. Set Kestrel port 5001 in `appsettings.Development.json` (`Kestrel:Endpoints:Http:Url=http://localhost:5001`).
7. `dotnet build /warnaserror` clean.
8. `dotnet run --project src/services/Lexio.Identity.Api` — verify `/healthz` returns 200.
9. Commit + open PR #13.

## Todo List
- [ ] Run `dotnet new lexio-service` for Identity
- [ ] Add 8 projects to `Lexio.slnx`
- [ ] Wire BuildingBlocks DI in `Program.cs`
- [ ] Configure port 5001
- [ ] `dotnet build /warnaserror` clean
- [ ] `/healthz` returns 200 locally
- [ ] PR opened, branch off `main`

## Success Criteria
- 8 projects compile with zero warnings.
- `/healthz` returns 200.
- OTel traces visible in local Jaeger (from compose stack).
- `Lexio.slnx` includes all 8 projects in deterministic order.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Template emits wrong namespace | M | L | Use template `--name` flag; verify with grep |
| slnx ordering churn on diff | M | L | Run `dotnet sln list` after add; sort alphabetically |
| Port 5001 conflict on dev machine | L | L | Document override via `LEXIO__KESTREL__PORT` env |

## Security Considerations
- No secrets in `appsettings.Development.json`; only Kestrel binding.
- Health endpoints public (no auth) — fine for liveness/readiness; do NOT expose `/healthz` with DB details.

## Next Steps
Unblocks phase-02 (Domain layer). Domain project must exist before User aggregate can be added.
