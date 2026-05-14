# Phase 06 — Api layer (minimal API endpoints, Swagger, ProblemDetails, CORS, rate limiting)

## Context Links
- researcher-04 §1 (all 8 endpoint contracts), §3.4 (rate limits), §4 (ProblemDetails)
- researcher-03 §1 (CORS — origins + credentials)
- BuildingBlocks: `Lexio.BuildingBlocks.Web` — `AddLexioWeb`, `UseLexioWeb`, ProblemDetails handler

## Overview
- Priority: P1
- Status: complete
- Effort: 3h
- Branch: `feat/be-identity-api` (off main, post-phase-05 merge)
- PR: see PR url after push

ASP.NET Core 10 minimal API. 8 endpoints per researcher-04 §1. Swagger via Scalar (preferred) or Swashbuckle. RFC 7807 ProblemDetails. Built-in `RateLimiter` middleware. CORS for FE origins with credentials.

## Key Insights
- Endpoint groups via `MapGroup("/api/v1/auth")` and `MapGroup("/api/v1/users")` + `/api/v1/roles`.
- Bearer auth from OpenIddict validation handler (phase-05). `RequireAuthorization()` on protected routes.
- `Results.Problem(...)` translates `Result<T>` errors to ProblemDetails via central `ResultExtensions.ToHttpResult`.
- Rate-limit policies: `login` policy = fixed-window 5/min keyed by request body email (or IP fallback); `default` = 100/min per IP; `authenticated` = 1000/min per `sub` claim.
- CORS exposes no extra headers but allows credentials (cookie comes from FE proxy in phase-10).
- **Ban enforcement (user-locked 2026-05-14):** access JWT carries a `banned` boolean claim (default `false`) issued by phase-05 token issuer. The Api uses a per-request `BannedUserAuthorizationHandler` that:
  1. Reads the `banned` claim from the principal; if `true` → 403 immediately.
  2. For **critical write endpoints** (PUT /users/me, POST /users/{id}/role), additionally consults a 60s in-process `IMemoryCache` keyed by `userId` (`ban:{userId}`); cache miss → single-row DB lookup → populate. This gives a DB-authoritative check on writes while light reads remain JWT-trusting (no DB roundtrip).
  3. On admin ban/unban (POST /users/{id}/role with role transition into/out of banned state, OR future dedicated ban endpoint), evict the cache entry: `cache.Remove($"ban:{userId}")`.
- Claim name: `banned` (lowercase, unscoped). Avoids `urn:lexio:*` verbosity; the access token audience already scopes the claim namespace. Document in OpenAPI security scheme description.

## Requirements
**Functional endpoints (per researcher-04 §1)**
| Method | Path | Auth | Rate-limit | Result mapping |
|---|---|---|---|---|
| POST | /api/v1/auth/register | anon | login | 201 / 409 / 422 |
| POST | /api/v1/auth/login | anon | login | 200 / 401 / 403 / 429 |
| POST | /api/v1/auth/refresh | anon (token in body) | login | 200 / 401 |
| POST | /api/v1/auth/logout | bearer | authenticated | 204 |
| GET  | /api/v1/auth/me | bearer | authenticated | 200 / 401 / 403 |
| PUT  | /api/v1/users/me | bearer | authenticated | 200 / 400 / 401 / 409 / 422 |
| POST | /api/v1/users/{id}/role | bearer + RequireAdmin | authenticated | 204 / 403 / 404 (deferred-active for completeness) |
| GET  | /api/v1/roles | anon | default | 200 |

**Non-functional**
- OpenAPI doc at `/openapi/v1.json`; Scalar UI at `/scalar/v1`.
- CORS allowed origins from `CORS_ALLOWED_ORIGINS` env (comma-separated).
- All error responses → ProblemDetails with `type` URI from `IdentityErrors`.

## Architecture
```
Lexio.Identity.Api/
├── Program.cs                              (DI wiring + middleware order)
├── Endpoints/
│   ├── AuthEndpoints.cs                    (5 routes)
│   ├── UserEndpoints.cs                    (2 routes)
│   └── RoleEndpoints.cs                    (1 route)
├── Authorization/
│   ├── BannedUserAuthorizationHandler.cs   (per-request claim + cache check)
│   ├── BannedUserRequirement.cs
│   └── BanStatusCache.cs                   (IMemoryCache wrapper, 60s TTL)
├── Configuration/
│   ├── CorsExtensions.cs
│   ├── RateLimitingExtensions.cs
│   ├── SwaggerExtensions.cs                (or ScalarExtensions)
│   ├── ProblemDetailsExtensions.cs
│   └── AuthorizationExtensions.cs          (registers BannedUser policy + handler + IMemoryCache)
└── appsettings.json / appsettings.Development.json
```

## Related Code Files
**Create:** ~9 cs files above.
**Modify:** `Lexio.Identity.Api/Program.cs` — wire `AddIdentityApplication()`, `AddIdentityInfrastructure()`, `AddIdentityOpenIddict()`, auth + authz, CORS, rate limiter, ProblemDetails, Scalar, endpoint mappings.
**Delete:** placeholder root `MapGet("/")` from phase-01.

## Implementation Steps
1. Wire `Program.cs` middleware order:
   ```
   UseHttpsRedirection (prod only)
   UseCors("lexio-fe")
   UseRateLimiter()
   UseAuthentication() // OpenIddict validation
   UseAuthorization()
   MapHealthChecks
   MapAuthEndpoints / MapUserEndpoints / MapRoleEndpoints
   MapScalarApiReference
   ```
2. Implement `AuthEndpoints.MapAuthEndpoints(IEndpointRouteBuilder)`:
   - `register`: body `RegisterRequest` → `RegisterUserCommand` via `IMediator.Send`; on success, `Results.Created($"/api/v1/users/me", response)` with refresh token in body.
   - `login`: extract `X-Forwarded-For` or `Connection.RemoteIpAddress` for `ipAddress`; send `LoginCommand`; map errors.
   - `refresh`: read raw token from body; send `RefreshTokenCommand`; return new access (and rotated refresh).
   - `logout`: read `sub` claim; send `LogoutCommand`; return 204.
   - `me`: read `sub`; send `GetMeQuery`; 200/403.
3. Implement `UserEndpoints` for `PUT /users/me` and `POST /users/{id}/role` (admin policy).
4. Implement `RoleEndpoints` for `GET /roles`.
5. `CorsExtensions.AddLexioCors(services, config)`:
   - Policy `lexio-fe`: `WithOrigins(config["CORS:AllowedOrigins"].Split(','))`, `AllowCredentials()`, `AllowAnyHeader()`, `WithMethods("GET","POST","PUT","DELETE","OPTIONS")`.
6. `RateLimitingExtensions.AddLexioRateLimits(services)`:
   - Fixed window `login`: 5 permits / 1 min, partition key = email-from-body or IP.
   - Fixed window `default`: 100 permits / 1 min, partition key = IP.
   - Fixed window `authenticated`: 1000 permits / 1 min, partition key = `sub` claim.
   - On rejected → 429 with `Retry-After` header.
7. `ProblemDetailsExtensions.AddLexioProblemDetails`:
   - Map `Result` error codes → `ProblemDetails.Type` URIs (e.g. `https://api.lexio.dev/errors/email-already-exists`).
   - Include `timestamp`, `instance` from `HttpContext.Request.Path`.
8. `SwaggerExtensions.AddLexioOpenApi`: configure Scalar UI; tag groups by endpoint group; document bearer scheme.
8a. `AuthorizationExtensions.AddLexioAuthorization`:
    - `services.AddMemoryCache()`.
    - Register `IAuthorizationHandler` → `BannedUserAuthorizationHandler`.
    - Define policy `NotBanned` requiring `BannedUserRequirement`; apply via `.RequireAuthorization("NotBanned")` on every bearer endpoint group.
    - Handler reads `banned` claim → if `true`, fail (returns 403 via ProblemDetails). On write endpoints, additionally calls `BanStatusCache.IsBannedAsync(userId)`:
      ```csharp
      return await cache.GetOrCreateAsync($"ban:{userId}", entry => {
          entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
          return userRepository.IsBannedAsync(userId);
      });
      ```
    - Light read endpoints (GET /auth/me, GET /roles) trust the claim — no DB roundtrip.
    - On admin ban/unban action (RoleChange command handler in Application): publish in-process notification → cache subscriber evicts `ban:{userId}`. For multi-instance deployments, accept eventual consistency within 60s TTL; document.
8b. Token issuer (phase-05) updated to include `banned` claim. Cross-phase reference — flag in PR.
9. Smoke-test via curl: register → login → me works end-to-end against local Postgres.
10. Api.Tests: `WebApplicationFactory<Program>` minimal test — `/healthz` returns 200, `/api/v1/roles` returns 5 entries.
11. PR #18 stacked on phase-05.

## Todo List
- [x] `Program.cs` complete wiring (DI + middleware order)
- [x] 8 endpoints implemented + ProblemDetails mapping
- [x] CORS policy from env
- [x] 3 rate-limit policies + `Retry-After`
- [x] `banned` claim authorization handler + `NotBanned` policy applied
- [x] `BanStatusCache` (IMemoryCache, 60s TTL) + invalidation on admin ban/unban
- [x] Scalar / OpenAPI UI
- [x] Curl smoke test of register→login→me→logout passes
- [x] Api.Tests minimal harness green
- [x] PR #18 opened

## Success Criteria
- `curl -X POST http://localhost:5001/api/v1/auth/register -d '{"email":"a@b.c","password":"P@ss1234","displayName":"A"}'` → 201 with tokens.
- Subsequent `POST /auth/login` → 200; `GET /auth/me` with bearer → 200.
- `POST /auth/login` with wrong password 6× rapidly → 429 with `Retry-After`.
- OpenAPI doc lists all 8 endpoints with response codes.
- CORS preflight `OPTIONS` from `http://localhost:3000` returns ACAO + ACAC=true.
- Banned user with valid JWT calling `PUT /api/v1/users/me` → 403 ProblemDetails (cache miss path); subsequent calls within 60s → 403 (cache hit, no DB roundtrip — verify via log + DB query count).
- Admin transition to/from banned state evicts cache: subsequent write request observes new status within one request, not after 60s.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Rate-limit partition key leaks email under load | M | M | `login` policy keys by IP+email tuple; raw email never logged in 429 response. |
| ProblemDetails leak stack traces in non-dev | M | H | Override `IncludeExceptionDetails = env.IsDevelopment()`. |
| Logout idempotency: double-call after token already revoked → 401 | L | M | LogoutCommandHandler returns success if no active tokens (idempotent per researcher-04 §1.1). |
| OpenAPI doc exposes deferred endpoints | L | L | Only map endpoints we implement; deferred ones absent from doc. |
| Banned user retains 15-min access window via stale JWT claim | M | M | Light reads accept up to 15min (JWT lifetime) of staleness. Write endpoints DB-check via cache (max 60s staleness). Acceptable: a banned user cannot mutate state beyond 60s; read leak is bounded by token lifetime. |
| `IMemoryCache` ban cache diverges across multi-instance deployments | M | M | Each instance has its own 60s window. Document: ban takes effect everywhere within 60s. For tighter SLA, future migration to Redis (out of scope). |
| Cache invalidation message lost if admin action handler crashes mid-flight | L | M | Worst case: 60s of stale ban status. Acceptable. Add a startup warmup or scheduled refresh later if needed. |

## Security Considerations
- Refresh token returned in JSON body — FE phase-10 swaps to httpOnly cookie via Next route handler (BE doesn't set cookie directly to keep BE stateless of FE cookie strategy).
- Bearer JWT validation via OpenIddict introspection handler (signature + iss + aud + exp).
- `/api/v1/users/{id}/role` requires `RequireAdmin` policy (5-role hierarchy check).
- Admin-only endpoint logs `RoleChanged` audit event with `changed_by_admin_id`.
- `/api/v1/roles` is public per researcher-04 §1.2 — consider gating in Phase 2 (open question in plan.md).

## Next Steps
Unblocks phase-07 (publishers on top of these endpoints' command flows) and phase-09 (compose wires the running service).
