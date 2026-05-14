# Phase 04 — Infrastructure: EF Core 10 + Postgres + outbox

## Context Links
- researcher-02 (full report — version pinning, value converters, soft-delete, outbox, migration commands)
- researcher-04 §2 (table schemas), §6.2 (outbox)
- `src/shared/Lexio.BuildingBlocks.Persistence/` — `LexioDbContextBase`, `IUnitOfWork`, soft-delete + outbox

## Overview
- Priority: P1
- Status: pending
- Effort: 3h
- Branch: `feat/be-identity-infra-ef` (off phase-03)
- PR: stacked PR #16

EF Core 10.0.10 + Npgsql 10.0.4 targeting **Postgres 18**. `IdentityDbContext` inheriting `LexioDbContextBase`. EntityTypeConfigurations. Initial migration creating `users`, `roles`, `refresh_tokens`, `oauth_connections`, plus `outbox_messages` (auto from base). Primary keys use **PG18 built-in `uuidv7()`** (no extension needed) for time-ordered UUIDs. Strong-typed `UserId(Guid)` and value converter remain unchanged. Roles seeded via **idempotent SQL script** (`infra/db/seed/identity-roles.sql`) executed inside a migration step — NOT EF `HasData`.

## Key Insights
- EF version pinned in `Directory.Packages.props` (foundation). Phase bumps from 9.0.10 to 10.0.10 if not already done — verify before migration add.
- DbContext options: `optionsBuilder.UseNpgsql(conn, npgsql => npgsql.SetPostgresVersion(18, 0)).UseSnakeCaseNamingConvention()`. Pinning PG version unlocks UUIDv7 and virtual generated columns at the Npgsql planner.
- All entity PKs default to PG-generated `uuidv7()` via `HasDefaultValueSql("uuidv7()")`. Strong-typed ID record-struct wraps the Guid on the CLR side; value converter is untouched.
- `UseSnakeCaseNamingConvention()` provides snake_case automatically; explicit `HasColumnName` only for divergences.
- Unique email index built on `lower(email_address)` expression for case-insensitive lookups. PG18 **virtual generated column** option available for `email_normalized = lower(email_address)` — apply if it removes app-side normalization; otherwise note as out-of-scope (the expression index alone is sufficient for lookup performance).
- `RefreshToken.TokenHash` indexed (unique) for O(1) refresh-lookup.
- `outbox_messages` already configured by `LexioDbContextBase.OnModelCreating(...)` — no extra config.
- Role seeding via idempotent SQL: `infra/db/seed/identity-roles.sql` uses `INSERT ... ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, permissions = EXCLUDED.permissions`. The 5 stable role UUIDs are hardcoded constants in the SQL file (also surfaced as `Role.SeedIds` constants in domain so app code can reference them). These IDs are stable identifiers worth an ADR.
- The migration that creates `roles` table also executes the seed script via `migrationBuilder.Sql(File.ReadAllText("infra/db/seed/identity-roles.sql"))` inside the migration's `Up`. Chosen over a runtime startup hook because: (a) seeding runs in the same `dotnet ef database update` step ops already runs, (b) rollback semantics align with migration `Down`, (c) reproducible across envs without app process needing to start.

## Requirements
**Functional**
- `IdentityDbContext` exposes `DbSet<User>`, `DbSet<Role>`, `DbSet<RefreshToken>`, `DbSet<OAuthConnection>`.
- `IUserRepository`, `IRoleRepository`, `IRefreshTokenRepository` implementations:
  - `EmailExistsAsync(email)` — case-insensitive.
  - `GetByEmailAsync(email)` — case-insensitive.
  - `GetByIdAsync(userId)` — returns `User?` (respects soft-delete filter).
  - `IRefreshTokenRepository.GetActiveByHashAsync(hash, clock)` — `SELECT ... FOR UPDATE` via `await ctx.Database.BeginTransactionAsync()` + raw FOR UPDATE for rotation safety.
- Migration `InitialIdentitySchema` produces 5 tables + outbox + seed roles + indexes.

**Non-functional**
- `dotnet ef migrations script 0 > infra/db/identity/initial-schema.sql --idempotent` produces a deterministic, idempotent script committed to repo.
- All datetime columns: `TIMESTAMP WITH TIME ZONE`.
- Foreign keys named `fk_<table>_<refTable>_<col>`.

## Architecture
```
Lexio.Identity.Infrastructure/
├── Persistence/
│   ├── IdentityDbContext.cs
│   ├── Configurations/
│   │   ├── UserConfiguration.cs              (PK default `uuidv7()`)
│   │   ├── RoleConfiguration.cs              (PK default `uuidv7()`; NO HasData — see infra/db/seed/identity-roles.sql)
│   │   ├── RefreshTokenConfiguration.cs      (PK default `uuidv7()`)
│   │   └── OAuthConnectionConfiguration.cs   (PK default `uuidv7()`)
│   ├── ValueConverters/
│   │   └── StrongTypedIdValueConverterSelector.cs
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   ├── RoleRepository.cs
│   │   └── RefreshTokenRepository.cs
│   ├── Interceptors/
│   │   └── AuditingInterceptor.cs            (already in BuildingBlocks; just wire)
│   └── Migrations/
│       └── 20260520_InitialIdentitySchema.cs (generated)
├── Time/SystemClock.cs                       (IClock impl)
└── DependencyInjection.cs                    (AddIdentityInfrastructure)
```

## Related Code Files
**Create:** ~10 cs files + 1 generated migration + 1 SQL idempotent-schema script in `infra/db/identity/initial-schema.sql` + 1 idempotent role seed at `infra/db/seed/identity-roles.sql`.
**Modify:**
- `Directory.Packages.props` — bump EF Core to 10.0.10, Npgsql to 10.0.4 (if foundation still on 9.x).
- `Lexio.Identity.Api/appsettings.Development.json` — add `ConnectionStrings:DefaultConnection`.

## Implementation Steps
1. Verify/bump EF + Npgsql versions in `Directory.Packages.props`.
2. Add `Microsoft.EntityFrameworkCore.Design` (PrivateAssets=all) to Infrastructure csproj for `dotnet ef` tooling.
3. Define `StrongTypedIdValueConverterSelector` covering 4 ID types. Value converter is unchanged — DB still stores `uuid`, CLR still wraps in `UserId(Guid)` etc.
4. Implement `IdentityDbContext : LexioDbContextBase`; constructor takes `(DbContextOptions, IClock)`; override `OnModelCreating` to call `base.OnModelCreating` then apply configs. In `OnConfiguring` (or DI options): `UseNpgsql(conn, b => b.SetPostgresVersion(18, 0))` + `UseSnakeCaseNamingConvention()`.
5. Write `UserConfiguration` per researcher-02 §8 template; `builder.Property(u => u.Id).HasDefaultValueSql("uuidv7()")`; expression index on `lower(email_address) UNIQUE`. (Optional) virtual generated `email_normalized` column — only if it actually removes domain-side normalization; otherwise skip per YAGNI.
6. Write `RoleConfiguration`: JSONB column for `permissions` using `HasColumnType("jsonb")` + `HasConversion` (List<string> ↔ JSON via `JsonSerializer`); **NO `HasData`** — seeding happens via SQL script in step 7a below.
7. Write `RefreshTokenConfiguration`: PK default `uuidv7()`; unique index on `token_hash`; `OnDelete(Cascade)` from User → RefreshToken; index on `(user_id, revoked_at)`.
   - Note: `revoked_at` semantics shift in phase-05 to "revoke-effective-at" with a 30s grace window. Column type/nullability unchanged (`TIMESTAMP WITH TIME ZONE NULL`); only the application-level interpretation differs. Cross-phase coupling — flag in PR.
7a. Author `infra/db/seed/identity-roles.sql`:
   ```sql
   -- Stable role UUIDs (DO NOT change — referenced by app code via Role.SeedIds)
   INSERT INTO roles (id, name, permissions, created_at, updated_at) VALUES
     ('<guest-uuid>',    'Guest',            '[...]', now(), now()),
     ('<learner-uuid>',  'Learner',          '[...]', now(), now()),
     ('<creator-uuid>',  'VerifiedCreator',  '[...]', now(), now()),
     ('<mod-uuid>',      'Moderator',        '[...]', now(), now()),
     ('<admin-uuid>',    'Admin',            '[...]', now(), now())
   ON CONFLICT (id) DO UPDATE
     SET name = EXCLUDED.name, permissions = EXCLUDED.permissions, updated_at = now();
   ```
   Fill the 5 placeholders with hardcoded UUIDs (generate once, never rotate). Mirror as `public static class Role.SeedIds` constants in `Lexio.Identity.Domain`.
8. Write `OAuthConnectionConfiguration`: PK default `uuidv7()`; unique `(provider, provider_user_id)`, FK to User cascade.
9. Implement repositories as thin wrappers around DbContext; expose `IUnitOfWork` from DbContext.
10. Implement `SystemClock : IClock`.
11. Wire DI in `AddIdentityInfrastructure(IServiceCollection, IConfiguration)`:
    - `AddDbContext<IdentityDbContext>` with Npgsql `SetPostgresVersion(18,0)`, snake_case naming.
    - Register repositories + `IUnitOfWork`.
    - Register `SystemClock`.
12. Generate migration:
    ```
    dotnet ef migrations add InitialIdentitySchema \
      --project src/services/Lexio.Identity.Infrastructure \
      --startup-project src/services/Lexio.Identity.Api \
      --output-dir Persistence/Migrations
    ```
13. Review generated migration: verify 5 tables + outbox + indexes present; PK columns have `DEFAULT uuidv7()` server-side. Manually edit the migration's `Up` to add: `migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "infra", "db", "seed", "identity-roles.sql")));` immediately after `roles` table creation. Mirror a `DELETE FROM roles WHERE id IN (...)` in `Down`. (Path-from-binary is fragile — alternative pattern: embed the SQL as a resource in the Infrastructure assembly. Pick whichever is cleaner in our repo layout; document the choice in the PR description.)
14. Generate idempotent SQL script: `dotnet ef migrations script 0 ... --idempotent --output infra/db/identity/initial-schema.sql`. The seed insert is inline-idempotent (`ON CONFLICT DO UPDATE`), so re-running the script is safe.
15. Apply locally + smoke-test via psql: `\dt`, `SELECT name FROM roles ORDER BY name;` returns 5; `SELECT id FROM users LIMIT 1` after insert returns a v7 UUID (timestamp-leading bytes).
16. Infrastructure.Tests: Testcontainers Postgres + apply migration + repository round-trip tests (defer most to phase-08; just smoke test here).
17. PR #16 stacked on phase-03.

## Todo List
- [ ] EF/Npgsql versions bumped
- [ ] DbContext `SetPostgresVersion(18,0)` + snake_case
- [ ] `IdentityDbContext` + 4 configurations (PKs default `uuidv7()`)
- [ ] Strong-typed ID converter selector (unchanged)
- [ ] `infra/db/seed/identity-roles.sql` authored with 5 stable UUIDs + `Role.SeedIds` constants
- [ ] 3 repository implementations
- [ ] `SystemClock`
- [ ] DI extension
- [ ] Initial migration generated + reviewed (PK defaults present)
- [ ] Migration `Up`/`Down` wired to seed SQL execution
- [ ] Idempotent SQL script committed
- [ ] Migration applied locally; 5 roles seeded; re-run is no-op
- [ ] Smoke integration test green (uuidv7 PK observable)
- [ ] PR #16 opened

## Success Criteria
- `dotnet ef database update` applies cleanly against fresh **Postgres 18** container.
- `SELECT count(*) FROM roles` returns 5; re-running the seed script is a no-op (`ON CONFLICT DO UPDATE` with same data).
- `SELECT id FROM users LIMIT 1` after insert returns a UUID whose first 6 bytes encode a recent timestamp (v7 ordering verified).
- `SELECT to_regclass('public.outbox_messages')` is non-null.
- `psql -c "\d users"` shows expected columns (`id uuid DEFAULT uuidv7()`) + indexes + soft-delete fields.
- Smoke test: register User → SaveChanges → reload by email returns same User.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| EF 9 → 10 upgrade breaks foundation tests | M | M | Run `dotnet test` on full solution after bump; foundation has no DB so impact is package-resolution only. |
| Role seed Guids change between migrations → FK drift | H | H | Hardcode 5 stable UUIDs in `infra/db/seed/identity-roles.sql` and mirror as `Role.SeedIds` constants. Never regenerate. Document in ADR. |
| `uuidv7()` not available on dev Postgres (someone runs PG ≤17) | M | H | `SetPostgresVersion(18,0)` declared; docker-compose pins `postgres:18`; phase-09 runbook documents minimum PG version. Migration fails fast with clear error if `uuidv7()` function missing. |
| Path lookup for seed SQL fails after publish (binary location differs from repo root) | M | M | Prefer embedded resource over filesystem path: add SQL as `EmbeddedResource` in Infrastructure csproj, read via `Assembly.GetManifestResourceStream`. Filesystem path is dev-only fallback. |
| snake_case convention conflicts with `__ef_migrations_history` | L | L | Override via `MigrationsHistoryTable("__ef_migrations_history","public")` in `UseNpgsql`. |
| JSONB List<string> converter breaks change tracking (whole-blob compare) | M | M | Configure `HasConversion` with `ValueComparer` that does element-wise compare. |
| Cascade delete of RefreshTokens on User soft-delete is unwanted | M | M | Soft-delete does NOT cascade (it's an update, not a delete); test confirms RefreshTokens survive User soft-delete. |

## Security Considerations
- Connection string read from env (`ConnectionStrings__DefaultConnection`); never committed.
- `password_hash` column type `TEXT` (bcrypt output ~60 chars; `TEXT` allows future Argon2 migration without column change).
- Migrations history table protected by Postgres role: app role only has `INSERT/UPDATE/SELECT` on `outbox_messages`; migrations run under separate admin role (documented in phase-09 runbook).

## Next Steps
Unblocks phase-05 (OpenIddict + password hasher impls plug into this DbContext) and phase-08 (integration tests use this DbContext via Testcontainers).
