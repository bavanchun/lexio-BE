# Phase 10 — FE swap step 1: route handlers + httpOnly cookie + CSP

## Context Links
- researcher-03 (full FE↔BE swap report — file-by-file matrix in §1)
- FE repo root: `/Users/vchun/Codes/My-projects/lexio-app/lexio-app-fe`
- Next.js 16 App Router + React 19 + TanStack Query + Zustand

## Overview
- Priority: P1
- Status: pending
- Effort: 3h
- Branch in FE repo: `feat/fe-identity-route-handlers` (off `main`)
- PR: FE PR #1 of identity-swap series (stacked PRs continue in phase-11)

Add Next.js route handler proxies (`/api/auth/sign-in|sign-out|me|refresh`) that forward to BE Identity at `NEXT_PUBLIC_LEXIO_API_URL`, parse JSON response, and set httpOnly+Secure+SameSite=Strict cookies for access + refresh tokens. CSP `connect-src` extended to include BE origin.

## Key Insights
- Tokens NEVER touch browser JS — only set/read by route handler via `cookies()` API.
- Route handlers are server-only; they can use `process.env.LEXIO_API_URL` (no `NEXT_PUBLIC_` prefix needed for server-side proxy URL — but we still expose one for client awareness/feature-flag reads).
- Two cookies: `lexio_access` (15-min, http-only, SameSite=Strict) + `lexio_refresh` (7-day, http-only, SameSite=Strict, Path=/api/auth/refresh).
- CSP additions in `next.config.ts`: keep `'self'`; add BE origin only for `connect-src` (route handlers run server-side, but client-side health pings may probe BE directly later — keep door open).
- Serwist (PWA): exclude `/api/auth/*` from caching (handled in phase-11 to keep this PR focused on server-side).
- Feature flag mechanism deferred to phase-11.

## Requirements
**Functional**
- POST `/api/auth/sign-in` (FE route handler):
  - Body `{ email, password }` → fetch BE `POST /api/v1/auth/login`.
  - On 200: parse `{ accessToken, refreshToken, user, expiresIn }`; set cookies; respond `{ user }` (no tokens in response body).
  - On 401: return `{ error: 'invalid-credentials' }` with status 401; no cookies set.
  - On 429: pass through with `Retry-After` header.
- POST `/api/auth/sign-out`:
  - Read `lexio_refresh` cookie; call BE `POST /api/v1/auth/logout` with bearer from `lexio_access`.
  - Clear both cookies regardless of BE response (idempotent).
  - Respond 204.
- GET `/api/auth/me`:
  - Read `lexio_access` cookie; call BE `GET /api/v1/auth/me`.
  - On 401: attempt refresh via internal `/api/auth/refresh` once; retry; if still 401 → clear cookies, respond 401.
  - On 200: respond with user JSON.
- POST `/api/auth/refresh` (internal — also callable on demand):
  - Read `lexio_refresh` cookie; call BE `POST /api/v1/auth/refresh`.
  - On 200: set rotated cookies.
  - On 401: clear cookies; respond 401.

**Non-functional**
- All route handlers use `runtime = 'nodejs'` (not edge — to access full cookie API + Node fetch).
- Set `Cache-Control: no-store` on all auth route responses.
- Log only correlation IDs server-side, never tokens.

## Architecture
```
apps/lexio-web/
├── app/
│   └── api/
│       └── auth/
│           ├── sign-in/route.ts
│           ├── sign-out/route.ts
│           ├── me/route.ts
│           └── refresh/route.ts
├── lib/
│   └── auth/
│       ├── cookies.ts                 (set/clear/read cookie helpers)
│       ├── be-client.ts               (typed fetch wrapper for BE)
│       └── errors.ts                  (FE error code mapping)
├── next.config.ts                      (CSP additions)
└── .env.example                        (BE URL placeholder)
```

## Related Code Files
**Create:** 4 route handlers + 3 lib files.
**Modify:**
- `apps/lexio-web/.env.example` — add `NEXT_PUBLIC_LEXIO_API_URL` + server-only `LEXIO_API_URL`.
- `apps/lexio-web/next.config.ts` — extend `connect-src` to include BE origin (dev: `http://localhost:5001`; staging/prod: `https://api.lexio.app`).
**Delete:** none (Zustand stub stays functional in this PR).

## Implementation Steps
1. Add env vars to `.env.example` + `.env.local` (developer setup).
2. Implement `lib/auth/cookies.ts`:
   ```ts
   export function setAuthCookies(access: string, refresh: string, expiresIn: number) { ... }
   export function clearAuthCookies() { ... }
   export function getAccess(): string | undefined { ... }
   export function getRefresh(): string | undefined { ... }
   ```
   Use `cookies()` from `next/headers`; flags: `httpOnly: true, secure: true (prod) | false (dev), sameSite: 'strict', path: '/'` (refresh: `path: '/api/auth/refresh'`).
3. Implement `lib/auth/be-client.ts`: typed `fetch` wrapper that prefixes `process.env.LEXIO_API_URL`, attaches bearer when provided, throws typed `BeError` on non-2xx.
4. Implement `lib/auth/errors.ts`: map BE ProblemDetails `type` URIs → FE error codes (`invalid-credentials`, `email-exists`, `weak-password`, `rate-limited`, `network`).
5. Write 4 route handlers per spec above. Use `NextResponse.json(...)` + `cookies()` API.
6. Update `next.config.ts` headers:
   ```ts
   { key: 'Content-Security-Policy', value: `default-src 'self'; connect-src 'self' ${apiOrigin}; ...` }
   ```
7. Smoke test locally with BE running: `curl -i -X POST http://localhost:3000/api/auth/sign-in -d '{"email":"a@b.c","password":"P@ss1234"}'` → 200 + Set-Cookie headers.
8. Unit-test cookie helpers + error mapper (Vitest).
9. Commit + open PR in lexio-app-fe.

## Todo List
- [ ] 4 route handlers
- [ ] cookies.ts + be-client.ts + errors.ts
- [ ] CSP updated with BE origin
- [ ] `.env.example` updated
- [ ] curl smoke test green
- [ ] Vitest unit tests on lib/auth helpers
- [ ] FE PR opened

## Success Criteria
- `curl -X POST /api/auth/sign-in` against running BE returns 200 + 2 cookies + `{ user: ... }`.
- DevTools Application tab shows `lexio_access` cookie marked HttpOnly + Secure (in prod build) + SameSite=Strict.
- CSP report-only mode shows no violations for in-app requests.
- Cookies cleared on `sign-out` even if BE returns 401 (idempotent).

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Refresh cookie path scoping breaks SPA navigation | M | M | Test: GET /api/auth/me with only `lexio_refresh` present (no access) → me handler must explicitly call refresh endpoint, not rely on browser auto-attach. |
| CSP blocks legitimate FE telemetry endpoint | L | M | Test in `Content-Security-Policy-Report-Only` first; flip to enforced after 1 sprint observation. |
| Refresh-token rotation race: two tabs hit /api/auth/me simultaneously, both attempt refresh, one fails | M | M | BE phase-05 FOR UPDATE lock makes one rotate succeed, the other gets 401 → handler retries once with new cookie. |
| SameSite=Strict breaks cross-site redirect logins (Google OAuth) | M | M | OAuth deferred to Phase 1.5 anyway. Document; revisit. |
| Browser blocks Set-Cookie on http:// localhost | L | L | Dev cert / Next dev server allows; verify in Chrome + Firefox. |

## Security Considerations
- Tokens isolated from JS → XSS cannot steal them.
- SameSite=Strict mitigates CSRF for state-changing routes.
- `secure: true` enforced in production builds via `process.env.NODE_ENV === 'production'`.
- No token logging; correlation ID only.
- Refresh cookie path scoped to `/api/auth/refresh` minimises exposure to other route handlers.

## Next Steps
Unblocks phase-11 (Zustand auth-store swaps to call these route handlers instead of the stub).
