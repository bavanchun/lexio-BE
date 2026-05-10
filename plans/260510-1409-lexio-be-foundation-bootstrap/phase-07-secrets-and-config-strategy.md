# Phase 07 — Secrets & configuration strategy

## Context Links
- Doc §5.3 (prod secrets — sealed-secrets + ESO, deferred)
- Phase 06 compose endpoints
- Microsoft.Extensions.Options.IOptions pattern

## Overview
- Priority: P1
- Status: pending
- Brief: Define how appsettings, user-secrets, .env, and (future) K8s secrets compose. No service Program.cs yet — strategy + a single canonical config sample lives in template (phase 08).

## Key Insights
- Convention: `appsettings.json` (committed defaults) → `appsettings.{Env}.json` (committed env-specific) → `appsettings.Local.json` (gitignored, per-dev) → user-secrets (gitignored, dev only) → environment variables (CI/CD, prod) → command-line args.
- `appsettings.Development.json` MAY be committed (no secrets).
- Strong-typed `IOptions<T>` with `[Required]` + DataAnnotations validation. `services.AddOptions<T>().BindConfiguration("X").ValidateDataAnnotations().ValidateOnStart()`.

## Requirements
- Functional:
  - `.env.example` (already from phase 06)
  - `docs/runbooks/configuration.md` describing layering + secret rotation
  - One canonical `appsettings.json` template (under `templates/Lexio.ServiceTemplate/content/Lexio.Service1.Api/`) deferred to phase 08, but documented here.
  - Strong-typed config classes pattern documented.
- NFR: zero secrets ever committed to git (verified via gitignore + `.gitleaks.toml` if added).

## Architecture
Layering precedence (lowest to highest):
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. `appsettings.Local.json` (gitignored)
4. User secrets (`dotnet user-secrets`, dev only)
5. Environment variables (e.g. `Database__ConnectionString`)
6. Command-line args

## Related Code Files
Create:
- `docs/runbooks/configuration.md`
- `docs/runbooks/local-development.md` (skeleton — completed in phase 12)
- `.gitleaks.toml` (optional secret-scan config; defer if no time)

Modify: `.gitignore` (already covers Local.json + .env).

## Implementation Steps
1. Branch `feat/be-config-strategy` off `feat/be-compose`.
2. Write `docs/runbooks/configuration.md`:
   - Layering table (above)
   - Per-developer setup: `cp .env.example .env`, `dotnet user-secrets init` (per service), `dotnet user-secrets set "Jwt:SigningKey" "..."`.
   - Strong-typed options pattern example:
     ```csharp
     public sealed record DatabaseOptions
     {
       [Required] public string ConnectionString { get; init; } = "";
       [Range(1, 100)] public int MaxPoolSize { get; init; } = 20;
     }
     // services.AddOptions<DatabaseOptions>()
     //   .BindConfiguration("Database")
     //   .ValidateDataAnnotations().ValidateOnStart();
     ```
   - Production guidance: K8s sealed-secrets + External Secrets Operator (placeholder section "TODO Phase 3 — Deployment").
   - Secret rotation: how to invalidate JWT signing key without downtime (out-of-scope for v0; section linked).
3. Skeleton `docs/runbooks/local-development.md` — sections (filled in phase 12):
   - Prerequisites (.NET 10 SDK, Docker, gh CLI)
   - First-time setup
   - Running a service locally
   - Connecting to compose stack
4. Optional `.gitleaks.toml` allowlisting `appsettings.json` defaults (no real secrets), blocking `.env`, `*.pfx`, `appsettings.Local.json`.
5. Commit: `docs(be-config): add secrets and configuration strategy runbook`.

## Todo List
- [ ] `configuration.md` runbook with layering precedence + IOptions pattern
- [ ] `local-development.md` skeleton
- [ ] Strong-typed options example documented
- [ ] Production-secrets path placeholder (sealed-secrets + ESO)
- [ ] gitignore confirmed to cover all secret files

## Success Criteria
- New developer can read `configuration.md` and stand up a service locally without asking the team.
- `git ls-files` after `dotnet user-secrets set` shows zero secret files.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Devs commit `appsettings.Local.json` accidentally | M | H | gitignore + add `.gitleaks` pre-commit hook in phase 10 |
| ENV var name collision (`Database__ConnectionString` vs `DATABASE_URL`) | M | M | Standardise on `Section__Key` double-underscore; document |
| User-secrets stored unencrypted on disk | L | M | Document — accept for dev only; never in CI |

## Security Considerations
- `appsettings.json` MUST NEVER contain real keys/passwords. Only structure + non-sensitive defaults.
- JWT signing keys: PUBLIC key in config, PRIVATE key in user-secrets/ENV only.
- Sealed-secrets / External Secrets path is a stated requirement for prod (doc §5.3) — placeholder only here.

## Next Steps
Unblocks phase 08 (template uses these patterns in its baseline `Program.cs` + `appsettings.json`).
