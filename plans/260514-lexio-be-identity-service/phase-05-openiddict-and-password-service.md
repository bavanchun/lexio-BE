# Phase 05 — OpenIddict 6 + bcrypt password service

## Context Links
- researcher-01 (OpenIddict 6 MIT recommendation)
- researcher-04 §3.2 (password rules), §7 (JWT structure), §10 (refresh-rotation question)
- `src/shared/Lexio.BuildingBlocks.Auth/` (existing JwtBearer placeholders)

## Overview
- Priority: P1
- Status: pending
- Effort: 4h
- Branch: `feat/be-identity-openiddict` (off phase-04)
- PR: stacked PR #17

OpenIddict 6 server (token issuance only — NOT full OAuth UI flows for MVP). RS256 signing with file-backed or X.509 dev cert. `BCryptPasswordHasher : IPasswordHasher`. `OpenIddictTokenIssuer : ITokenIssuer`. Refresh-token storage layered on top of `RefreshToken` entity from phase-02 (NOT OpenIddict's built-in token store — to keep schema explicit and audit-friendly).

## Key Insights
- OpenIddict 6 ships `Microsoft.AspNetCore.Authentication.JwtBearer` integration via `OpenIddict.Server.AspNetCore`.
- We use OpenIddict for **signing/validating JWTs only** (`AddOpenIddict().AddServer().UseAspNetCore().DisableTransportSecurityRequirement()` in dev). Refresh tokens flow through our own `RefreshToken` table — not OpenIddict's `OpenIddictToken` entity — so audit + cascade-on-user-delete stay first-class.
- Signing key: dev = ephemeral RSA dev cert (`AddDevelopmentSigningCertificate`). Staging/prod = X.509 cert from env-mounted PEM via `AddSigningCertificate(...)`.
- bcrypt: `BCrypt.Net-Next` (MIT) at cost 12. Verify-on-missing-user uses precomputed dummy hash to prevent timing enumeration.
- Refresh rotation: revoke old + issue new in same transaction. **30s grace window** is in scope (user-locked 2026-05-14): when a token is rotated, the old token's `revoked_at` column is set to `now + 30s` rather than `now`. The token-validation path treats the token as valid while `now < revoked_at`. This absorbs the race where mobile/PWA clients have in-flight requests that already attached the old refresh token; without the grace window, those concurrent calls would fail with 401 immediately after a successful rotation on another tab/device.
- The column type/nullability is unchanged (`TIMESTAMP WITH TIME ZONE NULL`); only the semantic of the value shifts from "moment of revocation" to "revoke-effective-at". A NULL value still means "active, never revoked". This is a cross-phase coupling with phase-04 (entity config) — flag in PR.

## Requirements
**Functional**
- `IPasswordHasher.Hash(string plaintext) → string` (bcrypt cost 12).
- `IPasswordHasher.Verify(string plaintext, string hash) → bool` (constant-time).
- `ITokenIssuer.IssueAccessTokenAsync(User, Role) → string` — RS256 JWT, 15-min expiry, claims per researcher-04 §7.1.
- `ITokenIssuer.IssueRefreshTokenAsync(User, ipAddress) → (rawToken, RefreshToken entity)` — 32-byte secure random, base64url, returned exactly once.
- `ITokenIssuer.RotateRefreshTokenAsync(currentRawToken, ipAddress) → Result<(access, refresh)>` — atomic revoke+issue.
- OpenIddict server registered at `/connect/token` (standard endpoint) for OAuth interop — internal commands also call `ITokenIssuer` directly.

**Non-functional**
- Dev cert auto-generated on first run; staging/prod cert path from `OPENIDDICT_SIGNING_CERT_PATH` env.
- JWT `iss` from `OPENIDDICT_ISSUER` env; `aud` from `OPENIDDICT_AUDIENCE` env.
- All signing keys rotated independently of access tokens (keys live across deploys).

## Architecture
```
Lexio.Identity.Infrastructure/
├── Security/
│   ├── BCryptPasswordHasher.cs
│   ├── OpenIddictTokenIssuer.cs
│   ├── RefreshTokenGenerator.cs           (32-byte CSPRNG → base64url)
│   └── SigningCertificateLoader.cs        (dev vs prod cert selection)
├── OpenIddict/
│   └── OpenIddictRegistrationExtensions.cs (AddIdentityOpenIddict)
└── DependencyInjection.cs                  (extended)
```

## Related Code Files
**Create:** 5 cs files above + appsettings keys `OpenIddict:Issuer`, `OpenIddict:Audience`, `OpenIddict:SigningCertPath`.
**Modify:** `Lexio.Identity.Infrastructure.csproj` adds `OpenIddict.Server.AspNetCore` + `OpenIddict.Validation.AspNetCore` + `BCrypt.Net-Next` (pinned in `Directory.Packages.props`).
**Delete:** none.

## Implementation Steps
1. Pin `OpenIddict.Server.AspNetCore 6.x`, `OpenIddict.Validation.AspNetCore 6.x`, `BCrypt.Net-Next 4.x` in `Directory.Packages.props`.
2. Implement `BCryptPasswordHasher`:
   - `Hash`: `BCrypt.HashPassword(plaintext, workFactor: 12)`.
   - `Verify`: try/catch around `BCrypt.Verify`; on any exception or null hash, run `BCrypt.Verify(plaintext, DummyHash)` so timing is constant. `DummyHash` is a static field pre-hashed at startup.
3. Implement `RefreshTokenGenerator` using `RandomNumberGenerator.GetBytes(32)` → `Base64UrlEncoder.Encode`.
4. Implement `OpenIddictTokenIssuer`:
   - Injects `IOpenIddictApplicationManager` or builds claims principal manually via `OpenIddictServerHandlers`.
   - `IssueAccessTokenAsync`: build `ClaimsIdentity` with claims (`sub`=UserId, `email`, `name`, `role`, `permissions` JSON array), wrap in `ClaimsPrincipal`, sign via OpenIddict, return JWT string.
   - `IssueRefreshTokenAsync`: generate raw 32-byte token, bcrypt-hash, persist `RefreshToken` entity via `user.IssueRefreshToken(hash, expiresAt, ip)`, return raw to caller.
   - `RotateRefreshTokenAsync`: open transaction → load token via `IRefreshTokenRepository.GetActiveByHashAsync` (FOR UPDATE) where "active" = `revoked_at IS NULL OR revoked_at > now()` (30s grace window honored) → set old token's `revoked_at = clock.UtcNow + TimeSpan.FromSeconds(30)` (the **revoke-effective-at** time) → issue new RefreshToken + access JWT → commit. On miss/expired/already-past-grace → return `IdentityErrors.InvalidToken` (401).
   - Token validation logic everywhere (lookup + middleware): treat a refresh token as valid when `revoked_at IS NULL OR revoked_at > now()`. Encapsulate as a single `RefreshToken.IsActive(IClock)` domain predicate to avoid duplication.
5. Implement `SigningCertificateLoader`:
   - `IHostEnvironment.IsDevelopment()` → `OpenIddictServerBuilder.AddDevelopmentSigningCertificate()`.
   - Else: load PEM from `OPENIDDICT_SIGNING_CERT_PATH`; if missing, throw on startup (fail-fast).
6. `AddIdentityOpenIddict`:
   - `AddCore().UseEntityFrameworkCore().UseDbContext<IdentityDbContext>()` — yes, OpenIddict needs its own tables (clients/scopes) even if we don't use them for refresh storage. Add to migration.
   - `AddServer().SetTokenEndpointUris("/connect/token").AllowPasswordFlow().AllowRefreshTokenFlow().UseAspNetCore().DisableTransportSecurityRequirement(dev only)`.
   - `AddValidation().UseLocalServer().UseAspNetCore()`.
7. **Add second migration** `AddOpenIddictTables` for OpenIddict's required tables (applications, authorizations, scopes, tokens). Keep separate from initial migration for clarity.
8. Wire authentication scheme in `Program.cs` (phase-06 actually does the Api wiring; this phase only configures the services).
9. Unit tests in Infrastructure.Tests:
   - `BCryptPasswordHasher` round-trip + reject malformed hash.
   - `RefreshTokenGenerator` produces 43-char base64url strings (32 bytes encoded).
   - `OpenIddictTokenIssuer` issues a JWT whose `sub`/`role` claims survive a parse+validate roundtrip using the same signing cert.
   - `RotateRefreshTokenAsync` grace window:
     - Rotate token at T0 → `revoked_at = T0 + 30s`.
     - At T0 + 10s, original raw token still authenticates (within grace).
     - At T0 + 31s, original raw token returns 401 (past grace).
     - Use a fake `IClock` to advance time deterministically.
10. PR #17 stacked on phase-04.

## Todo List
- [ ] OpenIddict + bcrypt pkg pins
- [ ] `BCryptPasswordHasher` (with timing-constant Verify)
- [ ] `RefreshTokenGenerator`
- [ ] `OpenIddictTokenIssuer` (issue + rotate with 30s grace)
- [ ] `RefreshToken.IsActive(IClock)` domain predicate (grace-window aware)
- [ ] `SigningCertificateLoader`
- [ ] `AddIdentityOpenIddict` extension
- [ ] `AddOpenIddictTables` migration
- [ ] Dev cert verified locally; JWT parseable
- [ ] Unit tests green
- [ ] PR #17 opened

## Success Criteria
- Local `dotnet run` issues a valid RS256 JWT verifiable at https://jwt.io with the dev public key.
- `Verify(plaintext, mismatchedHash)` returns false in O(1) time regardless of whether hash is malformed or correctly-formed-but-wrong (smoke benchmark).
- Refresh rotation test: rotate twice — old token accepted within 30s of rotation (grace), returns 401 after grace; newest token always accepted.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Dev cert in git accidentally | H | H | `.gitignore` for `*.pfx`, `*.pem`, `certs/`; `AddDevelopmentSigningCertificate` uses in-memory key — nothing to commit. |
| OpenIddict tables conflict with our schema names | L | M | Run migration locally first; OpenIddict prefixes its tables `OpenIddict*` by default — no conflict expected. |
| Refresh rotation race causes both tokens to be revoked | M | H | FOR UPDATE lock in `GetActiveByHashAsync` serialises concurrent rotations; second rotation sees revoked_at set and returns 401. |
| 30s grace window enables replay if attacker steals refresh token mid-rotation | L | M | Window is short (30s) vs token lifetime (7d). Attacker must steal during a 30s rotation interval AND use within it. Mitigation: log every successful "post-rotation grace use" as a security event (researcher-04 §5 audit topic); flag for review. Document trade-off in ADR-014. |
| bcrypt cost 12 too slow under load | L | M | Benchmark in phase-08 (target < 350ms per Verify on dev hardware). If problem, cache login result via short-lived token; do NOT lower cost. |
| Production signing cert leaked | L | C | Cert mounted at deploy time from secret store (documented in phase-09 runbook); cert rotation procedure documented in ADR-014. |

## Security Considerations
- Refresh tokens never logged (raw token only crosses process boundary in response body).
- Dummy hash for missing-user `Verify` is generated once at startup from a constant string; rotated never (timing-only purpose).
- `DisableTransportSecurityRequirement` ONLY in Development environment — production requires HTTPS at gateway.
- JWT `aud` claim validated by all downstream services (not just Identity).

## Next Steps
Unblocks phase-06 (Api endpoints call `ITokenIssuer` + `IPasswordHasher`).
