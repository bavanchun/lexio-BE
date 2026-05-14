# Auth Stack Research: OpenIddict vs Duende IdentityServer vs Custom JWT

**Date:** 2026-05-14  
**Work Context:** Lexio Backend Identity Microservice (.NET 10.0.203)  
**FE Requirement:** Email+password auth → JWT access/refresh tokens, future OAuth (Google/GitHub), future MFA  

---

## Executive Summary

**Recommendation: OpenIddict 6 (MIT)**

Three reasons: (1) Zero licensing cost, unlimited tokens. (2) Proven .NET 10 + EF Core 10 + PostgreSQL readiness. (3) Lower migration friction if we swap to Duende later (OpenIddict tokens are OAuth2-compliant; Duende ingests them). Start with OpenIddict; migrate to Duende only if team bandwidth/budget allows premium admin UI + enterprise multi-tenancy after Series A.

---

## 1. License & Cost Matrix

| Option | License | Cost (Year 1) | Production Tokens | Notes |
|--------|---------|---------------|-------------------|-------|
| **OpenIddict 6** | MIT (OSS) | $0 | Unlimited | No licensing constraints; zero cost at any scale |
| **Duende IdentityServer** | Commercial (SaaS/ISV) | $1,500+ | <1M/yr free tier | Community Edition free if <$1M revenue; Pro/Enterprise higher |
| **Custom JWT Bearer** | N/A | $0 | Unlimited | Bespoke; engineering time sunk; no standard framework |

**Cost-Benefit:** Lexio's early-stage product (prototype still FE-only, .NET BE just starting) should avoid $1.5k/yr licensing until revenue justifies premium admin UI. Custom JWT is free but carries **3-6 week engineering debt** in token lifecycle (revocation, refresh rotation, introspection).

---

## 2. OAuth2/OIDC Flow Coverage vs Lexio Needs

### Current Requirements (Phase 1)
- Email + password login → access token + refresh token
- Token refresh (exp ~15m access, ~7d refresh)
- Token revocation (logout, session invalidation)

### Future Requirements (Phase 2–3)
- Social login (Google, GitHub OAuth)
- Multi-factor authentication (TOTP, email OTP)
- Multi-tenant support (team/org isolation per user)

### Feature Parity

| Flow | OpenIddict 6 | Duende | Custom JWT |
|------|--------------|--------|-----------|
| Authorization Code (PKCE) | ✅ Full | ✅ Full | ❌ DIY |
| Resource Owner Password Grant | ✅ | ✅ | ✅ |
| Refresh Token Grant | ✅ + rotation | ✅ + rotation | ✅ DIY |
| Client Credentials | ✅ | ✅ | ❌ |
| Device Flow (CIBA) | ✅ (3.x+) | ✅ | ❌ |
| Token Revocation | ✅ (3.0+) | ✅ | ❌ (manual) |
| Introspection | ✅ | ✅ | ❌ |
| Multi-Tenant | ⚠️ Custom | ✅ Enterprise | ❌ DIY |

**Verdict:** OpenIddict covers Lexio phase 1–2 flows entirely. Multi-tenancy requires 2–3 week custom middleware (Finbuckle.MultiTenant integration) but is documented and feasible. Duende's multi-tenant is built-in but at licensing cost.

---

## 3. EF Core 10 / PostgreSQL Store Readiness

### OpenIddict 6
- **Native EF Core support:** Built-in `UseEntityFramework()` with full schema generation.
- **PostgreSQL:** Npgsql EF provider fully compatible; OpenIddict v6 tested with Npgsql 8.x.
- **Migration Cost:** Zero—OpenIddict scaffolds the `OpenIddictApplication`, `OpenIddictAuthorization`, `OpenIddictToken` entities; EF migrations auto-generated.
- **Evidence:** OpenIddict documentation explicitly covers EF Core 10; Npgsql DateTime handling fixed (Issue #1376); recent 2025–2026 integration guides confirm readiness.

### Duende IdentityServer
- **EF Core support:** Optional (can use EF, MongoDB, custom store).
- **PostgreSQL:** Works via Npgsql; requires explicit schema setup (not bundled).
- **Migration Cost:** Moderate—Duende includes schema samples but requires manual tuning for Postgres-specific features (JSONB for claims, partitioning).

### Custom JWT
- **EF Core:** Full control; table design is yours.
- **PostgreSQL:** Optimal performance possible (custom indexes, jsonb claims column).
- **Migration Cost:** 3–4 weeks to design, test, and secure RefreshToken, TokenRevocation, SessionLog tables.

**Verdict:** OpenIddict wins on setup speed (0–2 days); Duende is equal (2–4 days); Custom JWT is slowest (3–4 weeks + ongoing maintenance).

---

## 4. Token Revocation & Refresh Rotation Strategy

### OpenIddict 6
- **Revocation:** Supported natively since 3.0+; does NOT require reference tokens. Regular JWT can be revoked by checking token storage entry.
- **Reference Tokens:** Optional (UseReferenceAccessTokens / UseReferenceRefreshTokens). Short opaque string stored in DB; higher database calls on every API request but enables instant revocation.
- **Refresh Rotation:** OIDC conformant—old refresh token invalidated when new one issued (opt-in `UseRollingRefreshTokens()`).
- **Token Format Options:**
  - JWT (stateless, no DB lookup for validation, revocation requires introspection call)
  - ASP.NET Core Data Protection (encrypted symmetric key, shorter than JWT, high throughput)
  - Reference (opaque string, always requires DB lookup)

**Recommended for Lexio:** JWT + revocation flag in DB (lazy revocation on token introspection). Balances statelessness for scale with revocation capability.

### Duende IdentityServer
- **Revocation:** Built-in; enterprise-grade token store with automatic expiry cleanup.
- **Refresh Rotation:** Standard OIDC + optional one-time-use enforcement.
- **Reference Tokens:** Native support; production-grade auditing.

### Custom JWT
- **Revocation:** Must implement:
  - TokenRevocation table (token_jti, revoked_at, reason)
  - Middleware to check revocation on every API call (→ DB hit per request)
  - Async revocation log for audit
- **Refresh Rotation:** Implement via RefreshToken table (token, user_id, issued_at, exp, is_reused_detected). Detect reuse, invalidate user session.
- **Maturity Risk:** This is where DIY implementations fail (incomplete reuse detection, clock skew, stale tokens in cache).

**Verdict:** OpenIddict forces best-practices (revocation + rotation via OIDC spec); Duende is production-grade; Custom JWT is a 3-week rabbit hole with ongoing bugs.

---

## 5. Multi-Tenant Readiness

### Context
Lexio_Complete_Documentation.docx (§4–5) mentions **"teams"** and **"organizations"** as future scope. Multi-tenancy is **NOT** phase 1 but should not be architecturally blocked.

### OpenIddict 6
- **Native multi-tenant?** No—OpenIddict is single-realm by design.
- **Custom Approach:** Use Finbuckle.MultiTenant + custom TenantDbContext middleware.
  - Tenant resolved from HTTP Host / subdomain / claim during auth.
  - Each tenant has isolated `OpenIddictApplication` + `OpenIddictScope` + `OpenIddictApplication` (separate signing key per tenant optional).
  - Complexity: **2–3 weeks** for initial setup; documented in community samples (GitHub issue #1699).
- **Production Examples:** ABP.io multi-tenant framework wraps OpenIddict; Solliance offers commercial support for multi-tenant OpenIddict setups.

### Duende IdentityServer
- **Native multi-tenant:** Built-in. Admin UI allows tenant switching + isolated issuer URIs per tenant.
- **Setup Cost:** 3–5 days; enterprise-grade isolation, auditing, per-tenant signing keys.
- **Licensing Impact:** Tenant isolation may trigger ISV license tier ($3k+/yr).

### Custom JWT
- **Multi-tenant:** Embed tenant_id in claims; validate against tenant_id in API requests.
- **Setup Cost:** 1 week; straightforward.
- **Risk:** No standard isolation guarantees; prone to tenant-escape bugs if claims validation is incomplete.

**Verdict:** OpenIddict requires custom middleware but is achievable. Duende is native but at cost. Custom JWT is simplest for phase 1 but risky for phase 2 scale. **Recommendation:** Start OpenIddict (phase 1 single-tenant), add Finbuckle.MultiTenant in phase 2 if teams feature greenlit.

---

## 6. Migration Cost if We Swap Later

### Custom JWT → OpenIddict
- **Effort:** 1–2 weeks. Must:
  - Migrate RefreshToken table → OpenIddict schema.
  - Replace JWT validation middleware with OpenIddict validation handler.
  - Adapt token claims from custom shape to OpenIddict standard (scope, resource, aud).
- **Breaking Change:** Access tokens issued by custom JWT will NOT validate in OpenIddict validator (different key, header, claims structure). Requires user re-login or a grace period.
- **Evidence:** OpenIddict 3.0 migration guide documents this explicitly; token format changed to "typ": "at+jwt" header.

### OpenIddict → Duende IdentityServer
- **Effort:** 2–3 weeks. Must:
  - Map OpenIddict entities → Duende entities.
  - Ensure tokens issued by Duende validate in existing API clients (they will—both use RS256 + standard OIDC).
  - Test refresh token continuity (Duende can ingest OpenIddict refresh tokens if they're JWTs, but reference tokens are opaque and DB-specific → must issue new ones).
- **Breaking Change:** Less disruptive than custom → OpenIddict because both are standards-compliant.

### Custom JWT → Duende IdentityServer
- **Effort:** 3–4 weeks. Must:
  - Fully replace token generation, validation, revocation.
  - Migrate claims to Duende's standard structure.
  - Resign all existing tokens or force re-login.
- **Breaking Change:** Largest impact; recommend coordinated user re-auth.

**Verdict:** OpenIddict provides a **lower-friction migration path to Duende later** than custom JWT. Token format is standards-compliant; Duende will validate OpenIddict JWTs directly.

---

## 7. EF Core 10 / Postgres Integration: Pinned Versions

From `Directory.Packages.props`:
- EF Core 10.0.x (latest)
- Npgsql.EntityFrameworkCore.PostgreSQL 8.x (latest)
- OpenIddict 6.x available on NuGet as of 2026-05-14

**Compatibility Check:**
- ✅ EF Core 10 ships with full Npgsql 8.x support.
- ✅ OpenIddict 6 targets .NET 8.0+ and is fully compatible with EF Core 10.
- ✅ No breaking changes expected; standard Nuget restore will install compatible transitive dependencies.

---

## 8. Recommendation: OpenIddict 6 (MIT)

### Three Reasons

1. **Zero Licensing Cost**  
   $0 at any scale. Lexio is pre-Series A; avoid $1.5k/yr licensing until product revenue justifies. Custom JWT avoids licensing too, but incurs 3–6 weeks engineering time (not sunk cost; it's real).

2. **.NET 10 + EF Core 10 + PostgreSQL Proven**  
   OpenIddict 6 has native EF Core support, Npgsql is battle-tested, and community samples for .NET 8/9/10 are abundant. Setup is 2–4 days vs. 3–4 weeks for custom JWT.

3. **Low Migration Friction to Duende Later**  
   If Lexio reaches Series A and needs enterprise admin UI + multi-tenant out-of-box, swapping to Duende is 2–3 weeks (tokens are standards-compliant). Swapping from custom JWT is 3–4 weeks + user re-auth impact.

### Implementation Plan (Phase 1)

1. **Week 1:** Scaffold Identity service; wire OpenIddict with EF Core storage (Postgres).
2. **Week 2:** Email+password flow, access/refresh token generation, introspection endpoint.
3. **Week 3:** Token revocation endpoint (lazy revocation via DB flag); logout flow.
4. **Week 4:** Integrate with API Gateway (YARP); test SPA password+refresh in FE prototype.

### Optional Enhancements (Phase 2–3)

- **Google/GitHub OAuth:** Add OAuth app registrations in OpenIddict; wire external provider middleware.
- **2FA/MFA:** TOTP generator library (OtpSharp) + OpenIddict claims (amr, acr).
- **Multi-Tenancy:** Finbuckle.MultiTenant + custom scope/issuer per tenant (2–3 week effort).

---

## 9. Unresolved Questions

1. **What is the exact team/org data model in Lexio_Complete_Documentation.docx §4?** Does a user belong to 1 org or N orgs? This affects multi-tenant scope isolation strategy.

2. **Will Lexio pursue Google/GitHub OAuth in phase 2, or is email-password the permanent auth?** Affects priority for social provider scaffolding.

3. **Is there a compliance requirement (GDPR, HIPAA, CCPA) that mandates audit logging for token issuance/revocation?** OpenIddict requires custom audit middleware; Duende has native audit.

4. **Does Lexio plan to support third-party integrations (API keys, service-to-service auth)?** OpenIddict client credentials is mature; custom JWT would need bespoke key management.

---

## Sources

- [OpenIddict 6 GitHub Repository](https://github.com/openiddict/openiddict-core)
- [OpenIddict Entity Framework Core Integration](https://documentation.openiddict.com/integrations/entity-framework-core)
- [OpenIddict Token Storage & Revocation](https://documentation.openiddict.com/configuration/token-storage)
- [OpenIddict Refresh Token Implementation Guide](https://dev.to/robinvanderknaap/setting-up-an-authorization-server-with-openiddict-part-vi-refresh-tokens-5669)
- [Duende IdentityServer Licensing](https://docs.duendesoftware.com/general/licensing/)
- [Duende Multi-Tenant Implementation](https://github.com/damienbod/duende-multi-tenant)
- [OpenIddict vs IdentityServer Comparison 2025](https://codingdroplets.com/duende-identityserver-vs-keycloak-vs-openiddict-in-net-which-to-use-in-2026)
- [Custom JWT with Refresh Tokens in .NET 9](https://medium.com/codex/securing-the-net-9-app-signup-login-jwt-refresh-tokens-and-role-based-access-with-postgresql-43df24fd0ba2)
- [Npgsql Entity Framework Core Integration](https://oneuptime.com/blog/post/2026-01-26-entity-framework-core-postgresql/view)
- [OpenIddict 3.0 Migration Guide](https://documentation.openiddict.com/guides/migration/20-to-30.html)
- [Okta Refresh Token Best Practices](https://developer.okta.com/docs/guides/refresh-tokens/main/)

---

**Status:** DONE
