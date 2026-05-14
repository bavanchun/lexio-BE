# Phase 09 — docker-compose update + .env.example + secrets runbook

## Context Links
- Foundation phase-06 (`docker-compose.yml`) + phase-07 (secrets strategy)
- `docs/runbooks/` — existing runbook structure
- researcher-03 §2 — CORS + issuer env vars

## Overview
- Priority: P1
- Status: pending
- Effort: 2h
- Branch: `feat/be-identity-compose-secrets` (off phase-06; parallel to phase-07/08 in dependency graph)
- PR: stacked PR #21

Add `identity-api` service to compose. Wire env vars + healthcheck + dependencies (postgres, rabbitmq, kafka, otel-collector). Document secrets runbook: dev cert generation, prod cert mount, rotation procedure.

## Key Insights
- `identity-api` builds from `src/services/Lexio.Identity.Api/Dockerfile` (multi-stage: SDK build → ASP.NET runtime).
- Healthcheck hits `/healthz`; `depends_on` with `condition: service_healthy` for postgres + rabbitmq.
- OpenIddict signing cert: dev = generated on first container start by entrypoint script; prod = mounted from secret volume.
- Migrations run via separate one-shot container `identity-migrations` (image = same Dockerfile, command = `dotnet ef database update`) that runs before `identity-api` starts.

## Requirements
**Functional**
- `docker compose up identity-api` brings service online; `/healthz` returns 200 within 30s.
- `docker compose run --rm identity-migrations` applies migrations idempotently.
- `.env.example` lists all required env vars with safe defaults / placeholders.
- Runbook `docs/runbooks/identity-secrets.md` covers: dev cert, prod cert injection, rotation, key compromise procedure.

**Non-functional**
- All secrets read from env only — never baked into images.
- Image size < 250MB (use `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`).
- Compose service ordering: postgres → migrations → identity-api → others.

## Architecture
```
docker-compose.yml additions:
  identity-migrations:
    build: { context: ., dockerfile: src/services/Lexio.Identity.Api/Dockerfile, target: build }
    command: dotnet ef database update --project /src/services/Lexio.Identity.Infrastructure
    env_file: .env
    depends_on: { postgres: { condition: service_healthy } }
    restart: "no"

  identity-api:
    build: { context: ., dockerfile: src/services/Lexio.Identity.Api/Dockerfile }
    ports: ["5001:8080"]
    env_file: .env
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/healthz"]
      interval: 10s
      retries: 6
    depends_on:
      identity-migrations: { condition: service_completed_successfully }
      rabbitmq: { condition: service_healthy }
      kafka: { condition: service_started }
      otel-collector: { condition: service_started }
    volumes:
      - ./infra/certs/identity:/app/certs:ro
```

## Related Code Files
**Create:**
- `src/services/Lexio.Identity.Api/Dockerfile` (multi-stage).
- `src/services/Lexio.Identity.Api/.dockerignore`.
- `infra/certs/identity/.gitkeep` + `infra/certs/identity/README.md` (cert mount conventions; folder itself gitignored).
- `infra/db/identity/initial-schema.sql` already from phase-04.
- `docs/runbooks/identity-secrets.md`.

**Modify:**
- `docker-compose.yml` — add 2 services above.
- `.env.example` — add Identity env keys:
  ```
  IDENTITY__CONNECTIONSTRINGS__DEFAULTCONNECTION=Host=postgres;Port=5432;Database=lexio_identity;Username=lexio;Password=changeme
  IDENTITY__OPENIDDICT__ISSUER=http://localhost:5001
  IDENTITY__OPENIDDICT__AUDIENCE=https://app.lexio.dev
  IDENTITY__OPENIDDICT__SIGNINGCERTPATH=/app/certs/signing-cert.pfx
  IDENTITY__OPENIDDICT__SIGNINGCERTPASSWORD=
  IDENTITY__CORS__ALLOWEDORIGINS=http://localhost:3000
  IDENTITY__RABBITMQ__HOST=rabbitmq
  IDENTITY__RABBITMQ__USERNAME=guest
  IDENTITY__RABBITMQ__PASSWORD=guest
  IDENTITY__KAFKA__BOOTSTRAPSERVERS=kafka:9092
  ```
- Postgres init script `infra/postgres/init/01-create-lexio-identity-db.sql` — `CREATE DATABASE lexio_identity OWNER lexio;`.

## Implementation Steps
1. Write Dockerfile multi-stage:
   - `FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build` → restore via `Directory.Packages.props` → publish.
   - `FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine` → copy publish → `ENTRYPOINT ["dotnet","Lexio.Identity.Api.dll"]`.
   - Non-root user `app:app` (uid 1000).
2. Add `.dockerignore` for bin/obj/tests/plans.
3. Add Postgres init SQL for `lexio_identity` database.
4. Add 2 compose services per architecture above.
5. Update `.env.example` with placeholder values + clear comments distinguishing dev/prod.
6. Write `docs/runbooks/identity-secrets.md`:
   - **Section 1: Dev cert** — `dotnet dev-certs https --export-format Pfx -ep infra/certs/identity/signing-cert.pfx -p <pwd>`.
   - **Section 2: Staging/prod cert** — generate via `openssl req -x509 -newkey rsa:4096 -keyout ... -out ... -days 365`; store in secret manager (AWS SM / Azure KV / Vault); inject via deploy pipeline at `/app/certs/`.
   - **Section 3: Rotation** — quarterly cadence; document procedure: issue new cert → deploy both old + new → flip primary → revoke old after 7-day grace.
   - **Section 4: Compromise response** — revoke immediately, invalidate all refresh tokens (`UPDATE refresh_tokens SET revoked_at = NOW()`), force-relogin all users; document timing SLA (< 1h).
7. Compose smoke test: `docker compose up -d postgres rabbitmq kafka identity-api` → `curl localhost:5001/healthz` → 200.
8. PR #21 stacked on phase-06.

## Todo List
- [ ] Dockerfile multi-stage + non-root user
- [ ] `.dockerignore`
- [ ] Postgres init SQL for identity DB
- [ ] 2 compose services (migrations + api)
- [ ] `.env.example` updated
- [ ] Secrets runbook complete
- [ ] Local `docker compose up` smoke test passes
- [ ] PR #21 opened

## Success Criteria
- `docker compose up --build identity-api` → healthcheck green in < 30s.
- `docker compose logs identity-api` shows no errors, OTel traces flowing.
- `docker exec -it postgres psql -U lexio -d lexio_identity -c "SELECT count(*) FROM roles"` returns 5.
- `.env.example` documents every required key with example.
- Runbook reviewed; rotation procedure unambiguous.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Cert volume not mounted in prod → startup crash | M | H | Fail-fast in `SigningCertificateLoader` if `Production` + missing cert; document in runbook. |
| `.env` accidentally committed | M | C | `.gitignore` `.env` (already in foundation); add pre-commit hook `git-secrets` scan. |
| Alpine + .NET ICU issues | L | M | Install `icu-libs` in runtime image OR enable globalization-invariant mode (`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true`). |
| Migrations container races api startup | L | H | `depends_on: identity-migrations: { condition: service_completed_successfully }`. |
| Multi-arch image build slow on M1 | L | L | Use `--platform linux/amd64` explicit; document; CI runs amd64. |

## Security Considerations
- Cert PFX password stored in env, never in image; rotate quarterly.
- Compose default credentials (`guest`/`guest` for RMQ) are **dev only** — staging/prod uses unique creds per env.
- Container runs as uid 1000 (non-root); `/app/certs` mounted read-only.
- Runbook contains incident-response procedure for signing-key compromise.

## Next Steps
Unblocks phase-10 (FE points at running BE). FE devs can now `docker compose up identity-api` locally before swap.
