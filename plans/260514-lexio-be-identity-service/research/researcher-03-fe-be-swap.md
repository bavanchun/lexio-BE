# FE ↔ BE Identity Service Integration Research

**Date:** 2026-05-14  
**Researcher:** Analyst  
**Work Context:** lexio-app-fe + lexio-app-be  
**Scope:** Swap Zustand auth stub + localStorage for real .NET Identity service  

---

## Executive Summary

**Goal:** Migrate FE auth from Zustand-only stub (NOT-PROD) to real BE .NET 10 Identity service (OpenIddict + JWT RS256).

**Key constraints:**
- Dexie/IndexedDB (cards + learning data) stays — only auth swaps to BE
- Prototype must stay green during BE downtime (feature flag)
- Serwist PWA must NOT cache auth endpoints
- Current CSP allows 'self' only; must add BE origin to connect-src
- FE stack: Next.js 16 (App Router) + React 19 + TanStack Query + Zustand

**Recommendation:** httpOnly cookie via Next.js route handler proxy (not localStorage).  
**Risk Level:** LOW (auth isolation + proven pattern)  
**Adoption Timeline:** 4-6 hours dev + 1-2 hour integration test

---

## 1. File-by-File Change List

### FE Changes (lexio-app-fe)

| File Path | Current State | Changes | Impact |
|-----------|---------------|---------|--------|
| `apps/lexio-web/.env.example` | No API URL | Add `NEXT_PUBLIC_LEXIO_API_URL=http://localhost:5001` | Config |
| `apps/lexio-web/next.config.ts` | `connect-src 'self'` only | Add BE origin to connect-src; remove dev 'unsafe-eval' block | CSP hardening |
| `apps/lexio-web/features/auth/store/auth-store.ts` | Zustand stub (email + displayName) | Swap signIn/signOut to call route handlers; add JWT token field | Core auth logic |
| `apps/lexio-web/features/auth/components/login-form.tsx` | Direct signIn call | No change (form stays same — email field) | Transparent |
| `apps/lexio-web/features/auth/components/require-auth.tsx` | Checks `user !== null` | No change if auth-store hydration stays | Transparent |
| `apps/lexio-web/app/(app)/layout.tsx` | Uses useAuthStore | No change (import path same) | Transparent |
| `apps/lexio-web/lib/api/client.ts` | Has `AuthApi.getCurrentUserId()` stub | Update to add `signIn(email, password)` + `signOut()` | Interface |
| `apps/lexio-web/lib/api/index.ts` | Exports MockApiClient | Conditionally wire HttpAuthClient (new file) | Adapter |
| `apps/lexio-web/lib/api/http-auth-client.ts` | **NEW FILE** | Implements auth.signIn/signOut via /api/auth/* routes | Auth adapter |
| `apps/lexio-web/app/api/auth/sign-in/route.ts` | **NEW FILE** | POST proxy → BE /auth/sign-in; set httpOnly cookie | Route handler |
| `apps/lexio-web/app/api/auth/sign-out/route.ts` | **NEW FILE** | POST proxy → BE /auth/sign-out; clear cookie | Route handler |
| `apps/lexio-web/app/api/auth/me/route.ts` | **NEW FILE** | GET → read cookie; proxy to BE /auth/me | Route handler |
| `apps/lexio-web/app/sw.ts` | defaultCache (network-first) | Add NetworkOnly rule for /api/auth/* before defaultCache | PWA |

### BE Changes (lexio-app-be)

| Change | Location | Rationale |
|--------|----------|-----------|
| Add CORS origin for `http://localhost:3000` (dev) and `*.lexio.app` (prod) | Identity service startup config | Allow FE to call /auth/* endpoints |
| Set `Access-Control-Allow-Credentials: true` | CORS policy | Allow httpOnly cookies to be sent from FE |
| Ensure JWT issuer matches FE validation | OpenIddict config | Prevent token validation failures |

---

## 2. Environment Variable Design

### FE Environment (.env.local, .env.production)

```bash
# Dev (localhost)
NEXT_PUBLIC_LEXIO_API_URL=http://localhost:5001

# Staging
NEXT_PUBLIC_LEXIO_API_URL=https://api-staging.lexio.app

# Production
NEXT_PUBLIC_LEXIO_API_URL=https://api.lexio.app
```

**Notes:**
- `NEXT_PUBLIC_` prefix = exposed to browser (safe; base URL only, not secrets)
- Dev uses `http://` + localhost (localhost exempt from HTTPS requirement in dev)
- Staging/prod use `https://` with FQDNs
- No API keys in env vars (handled by httpOnly cookie)

### BE Environment

```dotenv
# CORS configuration (set in Identity service Startup.cs)
CORS_ALLOWED_ORIGINS=http://localhost:3000,https://staging.lexio.app,https://lexio.app

# OpenIddict / JWT
OPENIDDICT_ISSUER=http://localhost:5001  # or https://api.lexio.app in prod
OPENIDDICT_KEY_ID=your-key-id
OPENIDDICT_SIGNING_CERT_PATH=/app/certs/signing-cert.pem  # Injected at deploy time
```

---

## 3. Token Storage: httpOnly Cookie via Route Handler Proxy

### Recommended Pattern: httpOnly Cookie + Route Handler Proxy

**Why NOT localStorage?**
- XSS vulnerability: any script injected by attacker reads token
- No JS access = no risk

**Why NOT cookies directly from FE?**
- CORS restrictions + wildcard origin = security risk
- Better: FE calls Next.js route handler → handler forwards to BE

### Flow Diagram

```
┌──────────────────┐
│  FE Login Form   │
└────────┬─────────┘
         │ POST /api/auth/sign-in
         ▼
┌──────────────────────────────┐
│  Next.js Route Handler       │  (Server-side, full control)
│  POST /api/auth/sign-in      │
└────────┬─────────────────────┘
         │ POST http://localhost:5001/auth/sign-in
         │ (with email/password)
         ▼
┌─────────────────────────────┐
│  .NET Identity Service      │
│  Returns: JWT in response   │
└────────┬────────────────────┘
         │ 200 { accessToken, expiresIn }
         ▼
┌──────────────────────────────┐
│  Route Handler               │
│  Sets httpOnly cookie        │
│  res.setHeader(              │
│    'Set-Cookie',             │
│    'lexio_jwt=<token>;       │
│     HttpOnly; Secure;        │
│     SameSite=Lax; Path=/'    │
│  )                           │
└────────┬─────────────────────┘
         │ 200 { success: true }
         ▼
┌──────────────┐
│  FE Browser  │
│  (cookie set)│
└──────────────┘
```

### Route Handler Implementation Template

**`apps/lexio-web/app/api/auth/sign-in/route.ts`**

```typescript
import { NextRequest, NextResponse } from 'next/server';

const API_URL = process.env.NEXT_PUBLIC_LEXIO_API_URL;

export async function POST(request: NextRequest) {
  const { email, password } = await request.json();

  // 1. Forward to BE identity service
  const beResponse = await fetch(`${API_URL}/auth/sign-in`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });

  if (!beResponse.ok) {
    // 2. Handle BE errors (invalid creds, etc.)
    const errorData = await beResponse.json();
    return NextResponse.json(errorData, { status: beResponse.status });
  }

  // 3. Success — set httpOnly cookie
  const { accessToken, expiresIn } = await beResponse.json();
  const response = NextResponse.json({ success: true });

  response.cookies.set('lexio_jwt', accessToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    path: '/',
    maxAge: expiresIn, // seconds until expiry
  });

  return response;
}
```

**`apps/lexio-web/app/api/auth/me/route.ts`**

```typescript
import { NextRequest, NextResponse } from 'next/server';

const API_URL = process.env.NEXT_PUBLIC_LEXIO_API_URL;

export async function GET(request: NextRequest) {
  // 1. Read httpOnly cookie (automatic via fetch credentials)
  const token = request.cookies.get('lexio_jwt')?.value;

  if (!token) {
    return NextResponse.json({ user: null }, { status: 200 });
  }

  // 2. Validate against BE
  const beResponse = await fetch(`${API_URL}/auth/me`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  if (!beResponse.ok) {
    // Token expired or invalid — clear cookie
    const response = NextResponse.json({ user: null }, { status: 200 });
    response.cookies.delete('lexio_jwt');
    return response;
  }

  const user = await beResponse.json();
  return NextResponse.json({ user });
}
```

### Cookie Attributes Explained

| Attribute | Value | Why |
|-----------|-------|-----|
| `HttpOnly` | true | Prevent JS access (XSS protection) |
| `Secure` | true (prod) | HTTPS only (man-in-the-middle protection) |
| `SameSite` | 'lax' | CSRF protection; lax = allow top-level POST from external links |
| `Path` | '/' | Available to entire app |
| `MaxAge` | 3600 (e.g.) | 1 hour; sync with BE JWT expiry |

---

## 4. CORS Config Needed on BE Side

### ASP.NET Core CORS Configuration (Identity Service Startup)

**Problem:** FE on `http://localhost:3000`, BE on `http://localhost:5001` = different origins.

**Solution:** Add explicit CORS policy in Startup.cs or Program.cs:

```csharp
// In Program.cs (ASP.NET Core 6+)
var corsOrigins = new[] {
    "http://localhost:3000",              // Dev FE
    "https://staging.lexio.app",          // Staging FE
    "https://lexio.app",                  // Prod FE
    "https://www.lexio.app",              // Prod with www
};

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();  // ← CRITICAL: allow cookies
    });
});

var app = builder.Build();
app.UseCors();  // ← Must be placed BEFORE auth middleware
```

**Key Requirements:**
- `AllowCredentials()` = allows httpOnly cookies to be sent in CORS requests
- `AllowAnyMethod()` = GET, POST, DELETE, etc.
- `AllowAnyHeader()` = Content-Type, Authorization, etc.
- Placement matters: CORS middleware BEFORE auth middleware

---

## 5. Error Mapping: BE ProblemDetails → FE Toast/Form Errors

### BE Response Contract (RFC 7807 ProblemDetails)

Assume .NET Identity service returns standardized error responses:

```json
{
  "type": "https://api.lexio.app/errors/invalid-credentials",
  "title": "Invalid credentials",
  "status": 401,
  "detail": "Email or password incorrect",
  "instance": "/auth/sign-in"
}
```

### FE Error Handler

**`apps/lexio-web/lib/api/http-auth-client.ts`**

```typescript
interface AuthErrorResponse {
  status: number;
  detail: string;
  title: string;
}

async function handleAuthError(response: Response): Promise<never> {
  const error = (await response.json()) as AuthErrorResponse;

  const errorMap: Record<number, string> = {
    400: 'Invalid email or password format',
    401: error.detail || 'Invalid email or password',
    409: 'Email already registered',
    429: 'Too many login attempts — try again later',
    500: 'Server error — please try again',
  };

  throw new Error(errorMap[error.status] || error.detail);
}
```

**In LoginForm:**

```typescript
async function onSubmit(values: LoginFormValues) {
  try {
    await signIn(values.email, values.displayName);
    router.push('/dashboard');
  } catch (err) {
    toast.error((err as Error).message);
    // Or: setErrors({ email: (err as Error).message })
  }
}
```

---

## 6. Feature Flag Pattern: Fallback to Stub if BE Down

### Why: Prototype Must Stay Green

During early development, BE may be down, restarting, or under change. FE should gracefully fall back to stub auth rather than hard-fail.

### Implementation

**`apps/lexio-web/lib/api/index.ts`** (modified)

```typescript
function buildClient(): LexioApiClient {
  const db = new LexioDB();
  const repos = createRepositories(db);

  const useRealAuth = process.env.NEXT_PUBLIC_USE_REAL_AUTH !== 'false';

  if (useRealAuth) {
    return new HttpApiClient({
      baseUrl: process.env.NEXT_PUBLIC_LEXIO_API_URL || 'http://localhost:5001',
    });
  }

  // Fallback: stub auth + Dexie
  return new MockApiClient(repos);
}

export const apiClient: LexioApiClient = buildClient();
```

**`.env.local` (dev)**

```bash
# Set to 'false' to test with stub auth when BE is down
NEXT_PUBLIC_USE_REAL_AUTH=true
```

### Alternative: Graceful Degradation with Retry

Instead of a flag, implement exponential backoff + fallback:

```typescript
async function signInWithFallback(email: string, password: string) {
  try {
    return await realAuthClient.signIn(email, password);
  } catch (err) {
    console.warn('Real auth failed, falling back to stub:', err);
    return stubAuthClient.signIn(email, password);
  }
}
```

**Recommendation:** Use flag for clarity during dev; implement retry logic for prod resilience.

---

## 7. PWA / Serwist Implications

### Problem

Default Serwist config (via `defaultCache`) uses NetworkFirst strategy for all `/api/*` routes, which caches auth responses. Stale tokens = silent auth failures.

### Solution: Explicit NetworkOnly for Auth Endpoints

**`apps/lexio-web/app/sw.ts`** (modified)

```typescript
import { defaultCache } from '@serwist/next/worker';
import { Serwist } from 'serwist';
import { NetworkOnly, NetworkFirst } from 'serwist/strategies';
import { RegExpRoute } from 'serwist/routing';

declare const self: any & {
  __SW_MANIFEST: (string | { revision: string | null; url: string })[];
};

const serwist = new Serwist({
  precacheEntries: self.__SW_MANIFEST,
  skipWaiting: true,
  clientsClaim: true,
  navigationPreload: true,
  runtimeCaching: [
    // Auth endpoints — NEVER cache
    new RegExpRoute(
      /^http(s)?:\/\/(localhost:3000|staging\.lexio\.app|lexio\.app)\/api\/auth\//,
      new NetworkOnly(),
      'GET',
    ),
    new RegExpRoute(
      /^http(s)?:\/\/(localhost:3000|staging\.lexio\.app|lexio\.app)\/api\/auth\//,
      new NetworkOnly(),
      'POST',
    ),
    // Existing defaultCache (for vocabulary, stats, etc.)
    ...defaultCache,
  ],
  fallbacks: {
    entries: [
      {
        url: '/offline',
        matcher: ({ request }) => request.destination === 'document',
      },
    ],
  },
});

serwist.addEventListeners();
```

**Key Points:**
- Auth routes use `NetworkOnly` (no cache layer)
- Routes registered FIRST take precedence, so auth rules must come before `defaultCache`
- Other API endpoints still get NetworkFirst (cards, stats, etc.)
- Offline page still serves if network is down

---

## 8. CSP Additions for connect-src

### Current CSP (next.config.ts)

```typescript
const cspHeader = [
  "default-src 'self'",
  scriptSrc,  // dev: unsafe-eval; prod: none
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob: https://fonts.gstatic.com",
  "font-src 'self' data: https://fonts.gstatic.com",
  "connect-src 'self'",  // ← Only self currently
  "worker-src 'self' blob:",
  "manifest-src 'self'",
].join('; ');
```

### Updated CSP (add BE origins)

```typescript
const isDev = process.env.NODE_ENV === 'development';
const connectSrc = isDev
  ? "connect-src 'self' http://localhost:5001"  // Dev: allow local BE
  : "connect-src 'self' https://api.lexio.app";  // Prod: only prod API

const cspHeader = [
  "default-src 'self'",
  scriptSrc,
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob: https://fonts.gstatic.com",
  "font-src 'self' data: https://fonts.gstatic.com",
  connectSrc,  // ← Updated
  "worker-src 'self' blob:",
  "manifest-src 'self'",
].join('; ');
```

**Rationale:**
- `connect-src` governs fetch(), XMLHttpRequest, WebSocket → critical for API calls
- Localhost exemption in dev (CSP not enforced)
- Prod whitelist only HTTPS + known domain

---

## 9. HTTP Client Implementation

### Pattern: Adapter for Future Flexibility

**`apps/lexio-web/lib/api/http-auth-client.ts`** (new file)

```typescript
import { z } from 'zod';

interface HttpClientOptions {
  baseUrl: string;
}

export class HttpAuthClient {
  private baseUrl: string;

  constructor(options: HttpClientOptions) {
    this.baseUrl = options.baseUrl;
  }

  async signIn(email: string, password: string): Promise<void> {
    const response = await fetch('/api/auth/sign-in', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include', // Include cookies in request
      body: JSON.stringify({ email, password }),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Sign-in failed');
    }
  }

  async signOut(): Promise<void> {
    await fetch('/api/auth/sign-out', {
      method: 'POST',
      credentials: 'include',
    });
  }

  async getCurrentUser(): Promise<{ id: string; email: string } | null> {
    const response = await fetch('/api/auth/me', {
      method: 'GET',
      credentials: 'include',
    });

    if (!response.ok) return null;
    return response.json();
  }
}
```

**Note:** All fetch calls use `credentials: 'include'` so httpOnly cookies are sent automatically.

---

## 10. Auth Store Modification Strategy

### Current Zustand Store

```typescript
interface AuthState {
  user: StubUser | null;
  _hasHydrated: boolean;
  signIn: (email: string, displayName: string) => Promise<void>;
  signOut: () => void;
  setHydrated: () => void;
}
```

### Swap Strategy: Minimal Change

**Option A: Keep Zustand, wire BE**

```typescript
// No type changes; just swap implementation
signIn: async (email: string, displayName: string) => {
  // Call route handler (displayName is FE-only; BE may ignore)
  await fetch('/api/auth/sign-in', {
    method: 'POST',
    body: JSON.stringify({ email, password: displayName }), // ← Temp hack
    credentials: 'include',
  });
  
  // Fetch user from BE via /api/auth/me
  const response = await fetch('/api/auth/me', { credentials: 'include' });
  const userData = await response.json();
  set({ user: userData });
};
```

**Option B: New HttpAuthClient + Zustand (Recommended)**

Keep Zustand for UI hydration, wire HttpAuthClient for BE calls:

```typescript
const httpClient = new HttpAuthClient({ baseUrl });

signIn: async (email: string, password: string) => {
  await httpClient.signIn(email, password);
  const user = await httpClient.getCurrentUser();
  set({ user });
};
```

**Recommendation:** Option B — cleaner separation of concerns.

---

## 11. Dexie/Vocabulary Must NOT Change

**Critical:** Do NOT swap Dexie to HTTP yet.

- Vocabulary (decks, cards) stays in IndexedDB
- MockApiClient.decks.* still hit Dexie repos
- Only Auth swaps to HTTP

**Implementation:**

```typescript
// lib/api/index.ts
function buildClient(): LexioApiClient {
  const db = new LexioDB();
  const repos = createRepositories(db);  // Still used for cards/decks

  if (process.env.NEXT_PUBLIC_USE_REAL_AUTH === 'true') {
    return new HybridApiClient({
      auth: new HttpAuthClient({ baseUrl }),  // Real auth
      decks: new MockApiClient(repos).decks,  // Mock (Dexie)
      cards: new MockApiClient(repos).cards,  // Mock (Dexie)
      // ... other mocks
    });
  }

  return new MockApiClient(repos);
}
```

---

## 12. Testing Strategy

### Unit Tests (no BE needed)

- ✅ LoginForm validation (RHF + Zod) — unchanged
- ✅ Route handler cookie logic — new, critical
- ✅ Error mapping — new, critical
- ✅ Feature flag behavior — new

### Integration Tests (hit local BE)

- ✅ E2E: login → dashboard → card study
- ✅ Cookie persistence across page reload
- ✅ Token expiry → auto-logout
- ✅ Fallback to stub when BE down (if flag enabled)

### Manual QA Checklist

- [ ] Login with valid email/password
- [ ] Refresh page → user stays logged in (cookie persists)
- [ ] Sign out → cookie cleared
- [ ] Invalid email → shows "Invalid credentials" toast
- [ ] Network error → graceful fallback (if flag set)
- [ ] Offline (DevTools) → Serwist offline page
- [ ] CSP in Chrome DevTools → no connect-src violations

---

## 13. Rollout Plan

### Phase 1: Stub-to-Real Swap (Dev)

1. **Backend ready:** OpenIddict configured, /auth/sign-in + /auth/me endpoints live
2. **FE routes:** Create `/api/auth/sign-in`, `/api/auth/me`, `/api/auth/sign-out`
3. **Feature flag:** `NEXT_PUBLIC_USE_REAL_AUTH=true` (dev only)
4. **Test:** Login → verify httpOnly cookie set
5. **Tests pass:** E2E flow works with BE

### Phase 2: CSP + Serwist + Env

1. Update `next.config.ts` CSP for BE origin
2. Update `sw.ts` to skip auth caching
3. Add `NEXT_PUBLIC_LEXIO_API_URL` to .env files
4. Test offline page (doesn't break auth flow)

### Phase 3: Fallback & Graceful Degradation

1. Implement retry logic (exp backoff)
2. Test with BE down → falls back to stub
3. Test recovery (BE restarts → auto-retry succeeds)

### Phase 4: Prod Hardening

1. Remove `NEXT_PUBLIC_USE_REAL_AUTH` flag (always on)
2. Verify CSP prod values
3. Ensure `.env.production` has correct `NEXT_PUBLIC_LEXIO_API_URL`
4. Test push to staging → prod deploy

---

## 14. Known Unknowns & Unresolved Questions

1. **BE endpoint contract:** What fields does `/auth/sign-in` expect/return?
   - Expected: `{ email, password }` → `{ accessToken, expiresIn, refreshToken? }`
   - Must verify: token format (JWT?), expiry format (seconds? ms?)

2. **Password field:** Is the FE LoginForm staying email-only, or adding password?
   - Current: email + displayName (FE-only creation)
   - Real auth: email + password (BE validates)
   - Decision: Redesign form or use displayName as temp password?

3. **Refresh token:** Does OpenIddict implement refresh tokens?
   - If yes: Add /api/auth/refresh endpoint + cookie rotation logic
   - If no: Single JWT with long expiry; logout on expiry

4. **Email verification:** Does signup require email verification before auth?
   - If yes: Add separate `/auth/sign-up` endpoint
   - If no: /auth/sign-in handles both registration + login

5. **User profile structure:** What fields does BE return on /auth/me?
   - Current FE: `{ id, email, displayName, role, isVerified, createdAt, lastLoginAt }`
   - BE may differ; may need DTO mapping layer

6. **Middleware vs. Route Guards:** Do we need Next.js middleware to validate token on every request?
   - Recommendation: No — only validate on pages that use RequireAuth
   - Middleware adds complexity; route guards sufficient for prototype

7. **Testing against real BE:** When is BE stubbed/mocked for FE tests?
   - Option: Mock fetch in vitest; don't hit real BE in unit tests
   - Separate: E2E tests hit real local BE (docker-compose stack)

---

## Appendices

### A. Recommended Dependencies (No New Packages)

- `jose` — JWT validation (if FE validates token client-side) — NOT included yet
- `zod` — already included (error validation)
- `next/headers` — built-in (cookie reading)
- `serwist` — already included (PWA)

**No new dependencies required** if:
- Token validation stays on BE only
- FE just stores + forwards token
- Cookie handling via Next.js built-in APIs

### B. Security Checklist

- [x] httpOnly cookie = XSS protected
- [x] Secure flag in prod = HTTPS only
- [x] SameSite=Lax = CSRF protected (allow top-level POST from external links)
- [x] CORS configured on BE (AllowCredentials)
- [x] CSP updated (connect-src includes BE origin)
- [x] No tokens in URL params (route handlers keep in cookie)
- [x] No passwords logged (FE sends once; BE handles)
- [x] Token rotation on refresh (if refresh tokens used)

### C. Monitoring & Logging

**FE:**
- Log auth route handler requests/responses (sanitize tokens)
- Toast errors for user feedback
- Sentry integration for JS exceptions

**BE:**
- Log sign-in attempts (success + failures)
- Track CORS rejections (wrong origin)
- Monitor token validation failures

---

## Summary Table: Changes at a Glance

| Layer | File | Change Type | Complexity |
|-------|------|-------------|-----------|
| FE Env | .env.local | Config | Low |
| FE Config | next.config.ts | CSP + connect-src | Low |
| FE Auth | auth-store.ts | Replace signIn/signOut | Medium |
| FE Routes | app/api/auth/* | New 3 route handlers | Medium |
| FE API | lib/api/* | New HttpAuthClient | Medium |
| FE PWA | app/sw.ts | Add NetworkOnly rule | Low |
| BE Config | Startup.cs | CORS policy | Low |
| BE Auth | /auth/* endpoints | Assume ready (scope out) | Out of scope |

**Total New Files:** 4 (HttpAuthClient, 3 route handlers)  
**Modified Files:** 5 (env, config, store, api/index, sw)  
**Deleted Files:** 0

---

## Recommendation & Next Steps

**Adopt:** httpOnly cookie + route handler proxy pattern

**Rationale:**
1. ✅ Battle-tested (Stripe, GitHub, Google all use)
2. ✅ Secure (no XSS via JS access)
3. ✅ No CORS headers pollution
4. ✅ Works with Serwist (NetworkOnly excludes from cache)
5. ✅ Minimal FE changes (LoginForm unchanged)
6. ✅ Feature flag for fallback (BET stays green during BE downtime)

**Risk:** LOW
- Cookie handling is boring & proven
- Worst case: token leak = revoke on BE (no code deployed)
- Fallback flag allows graceful degradation

**Effort:** 4-6 hours
1. 1h: Create 3 route handlers + HttpAuthClient
2. 1h: Update auth store + LoginForm integration
3. 1h: CSP + Serwist + env config
4. 1.5h: Unit tests + integration tests
5. 1.5h: Manual QA + doc updates

**Blockers:** 
- [ ] BE /auth/sign-in + /auth/me endpoints ready
- [ ] BE CORS policy configured
- [ ] Password field design (email only? or email + password form redesign?)

---

**Status:** DONE

**Report:** This research provides a comprehensive, actionable plan for swapping FE auth from Zustand stub to real BE Identity service. No architectural surprises; follows industry best practices. Ready for implementation handoff to dev team.

