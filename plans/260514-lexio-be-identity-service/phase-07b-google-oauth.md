# Phase 07b — Google OAuth external login (BE-initiated)

## Context Links
- researcher-04 §1.4 (account linking flow), §6 (domain events + outbox)
- researcher-01 (OpenIddict 6 external-provider integration)
- Phase 02 (User aggregate, `OAuthConnection` entity)
- Phase 04 (`oauth_connections` table, outbox)
- Phase 05 (`ITokenIssuer` for issuing local access+refresh after Google handshake)
- Phase 06 (Api layer middleware + ProblemDetails)
- Phase 07 (outbox publishers — adds `OAuthConnectedEvent` consumer)
- `docs/runbooks/google-oauth-setup.md` (reference doc — author separately, NOT in this phase)

## Overview
- Priority: P1
- Status: pending
- Effort: 4h
- Branch: `feat/be-identity-google-oauth` (off phase-07)
- PR: stacked PR #19b
- Depends on: phase-06 (Api), phase-07 (outbox + Contracts project)

Add Google OAuth 2.0 as an external identity provider. Backend-initiated authorization-code flow (PWA hits BE start endpoint → BE redirects to Google → Google redirects back to BE callback → BE issues local access+refresh JWT pair via `ITokenIssuer`). PKCE-from-FE / direct-SPA flow is deferred to a later phase. Account linking: if Google email matches an existing user, append an `oauth_connections` row; if email is new, create User + OAuthConnection atomically with outbox-published `UserRegisteredEvent` + new `OAuthConnectedEvent`. GitHub deferred per user 2026-05-14.

## Key Insights
- OpenIddict 6 client integration uses `Microsoft.AspNetCore.Authentication.Google` (Google handler) wired into the OpenIddict server as an external authentication scheme. Code/token exchange handled by the Google handler; OpenIddict orchestrates the callback → local-token issuance.
- Account linking decision lives in domain: a single `LinkOrCreateFromExternalCommand(provider, providerUserId, email, displayName, ipAddress)` handler. It is the only entry point that touches `oauth_connections`; this keeps the linking invariants in one place.
- Atomicity: User insert + OAuthConnection insert + outbox messages (`UserRegisteredEvent` if new, `OAuthConnectedEvent` always) all in the same `IdentityDbContext.SaveChangesAsync` call → outbox guarantees at-least-once event publish post-commit.
- Secrets: dev uses `dotnet user-secrets` (`GoogleOAuth:ClientId`, `GoogleOAuth:ClientSecret`); staging/prod uses env vars (`GOOGLEOAUTH__CLIENTID`, `GOOGLEOAUTH__CLIENTSECRET`) mounted from secret store. Never in source/appsettings.
- Security: state parameter for CSRF protection (signed token with 5-min lifetime stored in cookie or in-memory cache, validated on callback). OIDC nonce for ID-token replay protection. Both managed by the Google handler if configured correctly.
- `OAuthConnectedEvent` joins the integration-events list in `Lexio.Identity.Contracts` (created in phase-07). Cross-link required.

## Requirements
**Functional**
- `GET /api/v1/auth/external/google/start?returnUrl={url}`:
  - Anonymous; rate-limit `login` policy.
  - Validates `returnUrl` against an allowlist (FE origins from `CORS:AllowedOrigins`).
  - Issues 302 to Google authorization URL with `state` + `nonce` + `scope=openid email profile`.
- `GET /api/v1/auth/external/google/callback`:
  - Anonymous; rate-limit `login` policy.
  - Validates `state` (anti-CSRF), exchanges code for tokens (via Google handler), reads claims (`sub`, `email`, `name`, `email_verified`).
  - If `email_verified === false` → 403 ProblemDetails (Google account must have verified email).
  - Dispatches `LinkOrCreateFromExternalCommand` via `IMediator.Send`.
  - On success: issues local access+refresh via `ITokenIssuer`; redirects to `returnUrl` with refresh-token strategy aligned with phase-06 (BE returns JSON in development; phase-10 FE proxy handles cookie issuance in production). For OAuth specifically, a one-time redirect with a short-lived signed code that FE exchanges for tokens is cleaner; spec this in PR.
- Application command: `LinkOrCreateFromExternalCommand`:
  - If `oauth_connections` row exists for `(provider, providerUserId)` → load linked User → return tokens. Updates `last_login_at`.
  - Else if a User exists with matching email (case-insensitive) → insert `OAuthConnection` row linking the existing User → publish `OAuthConnectedEvent` (NOT `UserRegisteredEvent` — user already existed).
  - Else → create new User + insert OAuthConnection in same transaction → publish `UserRegisteredEvent(provider='google')` AND `OAuthConnectedEvent`.
- Domain event: `OAuthConnectedEvent(UserId, Provider, ProviderUserId, ConnectedAt)` added to:
  - `Lexio.Identity.Domain/Events/` (domain event raised by `User.LinkOAuth(...)` / `User.CreateFromExternal(...)`).
  - `Lexio.Identity.Contracts/` (integration event, mapped via outbox in phase-07).

**Non-functional**
- All Google credentials read from configuration provider; never logged.
- ProblemDetails on every failure path; `type` URIs added to `IdentityErrors`: `oauth-state-invalid`, `oauth-email-unverified`, `oauth-provider-error`.
- Integration test uses **WireMock.Net** to stub Google token endpoint + userinfo endpoint; no calls to real Google in CI.

## Architecture
```
Lexio.Identity.Domain/
└── Events/
    └── OAuthConnectedEvent.cs                  (NEW domain event)

Lexio.Identity.Application/
└── Auth/External/
    ├── LinkOrCreateFromExternalCommand.cs
    ├── LinkOrCreateFromExternalCommandHandler.cs
    └── LinkOrCreateFromExternalCommandValidator.cs

Lexio.Identity.Infrastructure/
└── OAuth/
    ├── GoogleOAuthOptions.cs                   (POCO bound to GoogleOAuth:* config)
    └── GoogleOAuthRegistrationExtensions.cs    (AddGoogleOAuth — wires Google handler into OpenIddict)

Lexio.Identity.Api/
└── Endpoints/
    └── ExternalAuthEndpoints.cs                (2 routes: start, callback)

Lexio.Identity.Contracts/
└── OAuthConnectedEvent.cs                      (NEW integration event)
```

## Related Code Files
**Create:**
- `OAuthConnectedEvent.cs` (domain) + (contracts) — 2 files
- `LinkOrCreateFromExternalCommand` + handler + validator — 3 files
- `GoogleOAuthOptions.cs` + `GoogleOAuthRegistrationExtensions.cs` — 2 files
- `ExternalAuthEndpoints.cs` — 1 file
- Outbox mapper update for `OAuthConnectedEvent` (in phase-07's outbox-mapper file — small edit, see step 7 below)

**Modify:**
- `Lexio.Identity.Domain/Entities/User.cs` — add factory `CreateFromExternal(...)` and method `LinkOAuth(provider, providerUserId, clock)`; both raise `OAuthConnectedEvent`.
- `Lexio.Identity.Infrastructure/DependencyInjection.cs` — call `AddGoogleOAuth(config)`.
- `Lexio.Identity.Api/Program.cs` — map `ExternalAuthEndpoints`.
- `Directory.Packages.props` — add `Microsoft.AspNetCore.Authentication.Google` (current LTS) + `WireMock.Net` (test-only).
- `Lexio.Identity.Application/Errors/IdentityErrors.cs` — 3 new error codes.

**Delete:** none.

## Implementation Steps
1. Pin `Microsoft.AspNetCore.Authentication.Google` and `WireMock.Net` in `Directory.Packages.props`.
2. Author `OAuthConnectedEvent` in Domain + Contracts (mirroring `UserRegisteredEvent` pattern from phase-07).
3. Extend `User` aggregate:
   - `public static User CreateFromExternal(Email, DisplayName, RoleId, Provider, ProviderUserId, IClock)` — raises `UserRegisteredDomainEvent` AND `OAuthConnectedEvent`.
   - `public OAuthConnection LinkOAuth(string provider, string providerUserId, IClock)` — adds connection, raises `OAuthConnectedEvent`. Throws if already linked to same provider with different `providerUserId`.
4. Author `LinkOrCreateFromExternalCommand` + handler:
   - Single transaction: lookup connection → lookup user-by-email → branch (link / create / return-existing) → SaveChangesAsync (outbox messages collected by `LexioDbContextBase.CollectOutboxMessages`).
   - Returns `(User, IsNewUser)` for caller to decide whether to issue welcome flow.
5. Author `GoogleOAuthOptions` bound to `GoogleOAuth:` section:
   ```csharp
   public sealed class GoogleOAuthOptions
   {
       public string ClientId { get; init; } = "";
       public string ClientSecret { get; init; } = "";
       public string CallbackPath { get; init; } = "/api/v1/auth/external/google/callback";
   }
   ```
6. `GoogleOAuthRegistrationExtensions.AddGoogleOAuth(services, config)`:
   - Binds options.
   - `services.AddAuthentication().AddGoogle("Google", opts => { opts.ClientId = ...; opts.ClientSecret = ...; opts.CallbackPath = ...; opts.SaveTokens = false; opts.UsePkce = true; opts.SignInScheme = "External"; })`.
   - Registers cookie scheme `"External"` for the short-lived external auth ticket (5-min `ExpireTimeSpan`, `SecurePolicy = Always` in prod, `SameSite = Lax`).
7. Add mapper entry in phase-07's `IntegrationEventMapper` (or equivalent): `OAuthConnectedDomainEvent → OAuthConnectedEvent`. This is the only cross-phase code touch — confirm with phase-07 author on PR sequencing (07b stacks on 07).
8. Author `ExternalAuthEndpoints.MapExternalAuthEndpoints`:
   - `GET /start`: returnUrl allowlist check → `Results.Challenge(new AuthenticationProperties { RedirectUri = ... }, ["Google"])`.
   - `GET /callback`: `var result = await HttpContext.AuthenticateAsync("External");` → on `result.Succeeded` extract claims (`ClaimTypes.NameIdentifier` = Google sub, `ClaimTypes.Email`, `ClaimTypes.Name`, `email_verified`) → dispatch command → issue tokens via `ITokenIssuer` → sign-out external scheme → 302 to returnUrl with code parameter.
9. Wire DI: `Program.cs` calls `AddGoogleOAuth(builder.Configuration)`; endpoints mapped after `MapAuthEndpoints`.
10. Add 3 entries to `IdentityErrors` + corresponding ProblemDetails type URIs.
11. Integration test (`Lexio.Identity.IntegrationTests/ExternalAuth/GoogleOAuthFlowTests.cs`):
    - WireMock.Net stubs Google `/o/oauth2/v2/auth`, `/token`, and `/oauth2/v3/userinfo`.
    - Override `GoogleDefaults.AuthorizationEndpoint` / `TokenEndpoint` / `UserInformationEndpoint` to WireMock URLs in a test-specific config override.
    - Scenarios:
      - New email → user created → row in `users` + `oauth_connections` + 2 outbox messages.
      - Existing email → user linked → row added to `oauth_connections` only + 1 outbox message (`OAuthConnectedEvent`).
      - Existing connection → returns same user, updates `last_login_at`.
      - Unverified email → 403 ProblemDetails with `type` ending in `oauth-email-unverified`.
      - Tampered `state` → 401.
12. Smoke test locally with a real Google OAuth client (developer credential in user-secrets); document credential setup in `docs/runbooks/google-oauth-setup.md` (separate doc, not authored here — link only).
13. PR #19b stacked on phase-07.

## Todo List
- [ ] Pkg refs added (`Microsoft.AspNetCore.Authentication.Google`, `WireMock.Net`)
- [ ] `OAuthConnectedEvent` domain + contracts
- [ ] `User.CreateFromExternal` + `User.LinkOAuth` with event raising
- [ ] `LinkOrCreateFromExternalCommand` + handler + validator
- [ ] `GoogleOAuthOptions` bound from config
- [ ] `AddGoogleOAuth` DI extension + cookie scheme `"External"`
- [ ] Outbox mapper updated for `OAuthConnectedEvent` (phase-07 file)
- [ ] `ExternalAuthEndpoints` (start + callback) with `returnUrl` allowlist
- [ ] `IdentityErrors` 3 new codes + ProblemDetails URIs
- [ ] WireMock.Net integration tests cover 5 scenarios
- [ ] User-secrets configured locally; manual Google flow works end-to-end
- [ ] PR #19b opened

## Success Criteria
- `GET /api/v1/auth/external/google/start` 302s to `https://accounts.google.com/o/oauth2/v2/auth?...` with `state`, `code_challenge` (PKCE), `nonce`, `scope=openid email profile`.
- After Google consent, callback issues a local access+refresh JWT pair valid against the existing `/api/v1/auth/me` endpoint.
- `oauth_connections` row present with `(provider='google', provider_user_id=<google-sub>)` unique constraint enforced.
- Outbox emits exactly: `UserRegisteredEvent` + `OAuthConnectedEvent` for first-time Google sign-in; only `OAuthConnectedEvent` for linking an existing email.
- Unverified-email Google account → 403 with ProblemDetails `type: https://api.lexio.dev/errors/oauth-email-unverified`.
- Tampered/missing `state` → 401.
- All 5 integration test scenarios pass with WireMock.Net stubs; CI never hits real Google.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Account-takeover via unverified Google email matching existing local user | M | H | Reject `email_verified === false` at callback (403). For verified emails, linking to existing user is intended behavior — researcher-04 §1.4 confirms. |
| `state` validation bypass (CSRF) | L | H | Use Google handler's built-in state correlation + signed `External` cookie scheme; reject on mismatch with explicit test case. |
| Google client secret committed to repo | H | C | `dotnet user-secrets` in dev; env vars in prod; `.gitignore` covers `secrets.json`; pre-commit hook scans for secret patterns (existing). |
| Token issuance after Google callback differs from password flow (claim drift) | M | M | Funnel through the same `ITokenIssuer.IssueAccessTokenAsync(User, Role)` path; no duplicate claim-building logic. Add architecture test asserting only one caller of `IssueAccessTokenAsync` exists per phase-05 path + phase-07b path. |
| `returnUrl` open redirect | M | H | Allowlist match against `CORS:AllowedOrigins` parsed at startup; reject any non-matching `returnUrl` with 400 before initiating the Google challenge. |
| OAuth connection orphaned by soft-deleted user | L | M | `OAuthConnection` cascade-deleted only on hard delete; soft-deleted users retain their connections so re-activation re-links cleanly. Test confirms. |

## Security Considerations
- `state` (CSRF), `nonce` (replay), PKCE `code_challenge` (intercepted-code) all enabled. PKCE is enabled even though this is BE-initiated — it costs nothing and aligns with deferred SPA flow.
- Refresh tokens issued by Google are NOT stored: `SaveTokens = false`. Lexio operates on its own access+refresh token lifecycle post-handshake.
- `External` cookie scheme is short-lived (5 min) and only used to bridge Google redirect to local-token issuance.
- Client secret never logged: configured via `Configuration` provider; ensure `appsettings*.json` checked for accidental commits during PR review.
- Audit event `UserLoggedIn(provider='google')` published via outbox → Kafka `vocab.audit-log` (phase-07 audit topic includes provider field per researcher-04 §5).

## Next Steps
Unblocks phase-08 (test infra picks up WireMock.Net + new integration test class) and phase-09 (docker-compose adds `GOOGLEOAUTH__*` env vars + runbook reference). PKCE-from-SPA / direct-from-FE Google flow deferred to a post-MVP phase if FE team wants to skip the BE proxy redirect.
