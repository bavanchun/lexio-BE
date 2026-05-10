# Configuration & Secrets Runbook

## Layering Precedence (lowest → highest)

| # | Source | Committed? | Notes |
|---|--------|-----------|-------|
| 1 | `appsettings.json` | Yes | Structure + non-sensitive defaults only |
| 2 | `appsettings.{Environment}.json` | Yes | Env-specific non-secret config |
| 3 | `appsettings.Local.json` | **No** (gitignored) | Per-developer overrides |
| 4 | User secrets (`dotnet user-secrets`) | **No** | Dev only; stored in `~/.microsoft/usersecrets/` |
| 5 | Environment variables | **No** | CI/CD, containers, prod |
| 6 | Command-line args | — | Rarely used; highest priority |

Higher number wins. Environment variables use double-underscore for hierarchy:
`Database__ConnectionString` maps to `Database:ConnectionString`.

## Per-Developer First-Time Setup

```bash
# 1. Clone and enter repo
git clone git@github.com:bavanchun/lexio-BE.git && cd lexio-BE

# 2. Copy environment template
cp .env.example .env            # Docker Compose reads this

# 3. Start the polyglot dev stack
docker compose up -d

# 4. Install dotnet tools (Husky, etc.)
dotnet tool restore

# 5. Set user secrets per service (example for Identity service)
cd src/services/Identity/Lexio.Identity.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<your-local-rsa-private-key>"
dotnet user-secrets set "Database:ConnectionString" "Host=localhost;Port=5432;Database=identity_db;Username=lexio;Password=devpass"
```

## Strong-Typed Options Pattern

All configuration sections are bound to validated options classes:

```csharp
// Options class — lives in the service's Application or Infrastructure layer
public sealed record DatabaseOptions
{
    [Required] public string ConnectionString { get; init; } = "";
    [Range(1, 100)] public int MaxPoolSize { get; init; } = 20;
}

// Registration in Program.cs or DependencyInjection.cs
services.AddOptions<DatabaseOptions>()
    .BindConfiguration("Database")
    .ValidateDataAnnotations()
    .ValidateOnStart();  // Fail fast at startup, not first use
```

Naming convention: `{Concern}Options` with a matching `appsettings.json` section key `{Concern}`.

## appsettings.json Contract

```json
{
  "Database": {
    "ConnectionString": "",   // required — set via user-secrets / ENV
    "MaxPoolSize": 20
  },
  "Redis": {
    "ConnectionString": ""    // required — set via user-secrets / ENV
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "",           // required
    "Password": ""            // required
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "Jwt": {
    "Authority": "",          // OIDC provider URL
    "Audience": ""            // Service audience identifier
  },
  "Otel": {
    "Endpoint": ""            // Optional — OTLP collector endpoint
  }
}
```

Empty strings signal "required at runtime"; startup validation catches missing values.

## Secret Naming Rules

- Use `Section__Key` (double-underscore) for environment variables.
- Never use `DATABASE_URL` (12-factor style) — Lexio uses `Database__ConnectionString`.
- JWT: **Public key** in config; **private/signing key** in user-secrets/ENV only.

## Production Secrets (Placeholder — Phase 3 Deployment)

Production uses Kubernetes Sealed Secrets + External Secrets Operator (ESO):

1. DevOps encrypts secret values with `kubeseal`.
2. Encrypted `SealedSecret` CR is committed to the GitOps repo (safe to commit).
3. Sealed Secrets controller decrypts → creates K8s `Secret` in cluster.
4. ESO syncs K8s `Secret` values into pod environment variables.

See doc §5.3 for the full spec. No implementation here — placeholder only.

## Secret Rotation

- **JWT signing key**: rotate by updating the secret in the vault + rolling restart of all services.
  - Zero-downtime: configure two valid keys during rotation window (OIDC JWKS supports multiple).
  - Lexio uses OIDC authority → key rotation is handled by the identity provider.
- **Database passwords**: `ALTER ROLE lexio PASSWORD '...'` + restart services (or connection pool reconnect).
- **Redis**: `CONFIG SET requirepass ...` + update `Redis:ConnectionString`.

## Gitignore Verification

The following are already in `.gitignore`:
```
.env
appsettings.Local.json
*.pfx
*.p12
```

Run `git ls-files` after any secret operation to confirm no secrets are staged.
