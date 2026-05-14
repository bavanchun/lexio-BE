# Phase 11 — FE swap step 2: replace Zustand stub + Serwist exclusion + feature flag

## Context Links
- researcher-03 §1 (auth-store + api-client matrix), §4 (Serwist exclusion)
- Phase 10 route handlers
- FE files (researcher-03 §1): `features/auth/store/auth-store.ts`, `lib/api/client.ts`, `lib/api/index.ts`, `lib/api/http-auth-client.ts`, `app/sw.ts`

## Overview
- Priority: P1
- Status: pending
- Effort: 3h
- Branch in FE repo: `feat/fe-identity-store-swap` (off `feat/fe-identity-route-handlers`)
- PR: stacked FE PR #2

Swap Zustand stub `signIn(email, displayName)` for real call to `/api/auth/sign-in`. Add `HttpAuthClient` ApiClient implementation. Add Serwist `NetworkOnly` rule for `/api/auth/*`. Add **runtime** feature flag via Zustand store `useAuthConfigStore` (persisted to localStorage; values `'stub' | 'real'`, default `'real'`) — no build-time env var. Add dev-only debug-panel toggle to flip the flag without rebuild. Map BE errors to user-facing toasts.

## Key Insights
- Zustand store currently holds `user: { email, displayName }`. New shape: `user: { id, email, displayName, role, isVerified }` matching BE DTO; no token field (tokens live in cookies only).
- Hydration on app boot: call `/api/auth/me` once → if 200, set user; if 401, leave null and redirect to `/sign-in`.
- Serwist (`app/sw.ts`): `defaultCache` is NetworkFirst; insert `NetworkOnly` rule BEFORE defaultCache for URL pattern `/^\/api\/auth\//`.
- **Runtime feature flag (user-locked 2026-05-14):** new Zustand store `useAuthConfigStore` with shape `{ authBackend: 'stub' | 'real' }`, wrapped in `persist` middleware (localStorage key `lexio:auth-config`). Default `'real'`. Replaces the prior `NEXT_PUBLIC_AUTH_BACKEND` env var entirely.
- Call-site branching: `lib/api/index.ts` exports a getter (NOT a singleton) — `function getAuthClient() { return useAuthConfigStore.getState().authBackend === 'real' ? httpAuthClient : mockApiClient.auth; }`. Or wrap in a React hook for components. This shifts the branch from build-time to per-call, enabling live toggle.
- Dev-only debug-panel toggle: a small floating component rendered only when `process.env.NODE_ENV === 'development'` (Next.js dead-code-eliminates the import in production bundles). Exposes a button to flip `authBackend` and clear Zustand auth-store + TanStack Query cache. Hidden in production builds.
- Persist note: because the flag is in localStorage, it survives reloads (intended). It does NOT survive cache clears or incognito — fallback default `'real'` covers that.
- Error toast strings live in `features/auth/errors.ts` with i18n-ready string keys; mapped from BE error codes via shared `lib/auth/errors.ts`.

## Requirements
**Functional**
- New file `lib/api/http-auth-client.ts`:
  - Implements `AuthApi` interface (already exists in `lib/api/client.ts`).
  - `signIn(email, password)`: POST `/api/auth/sign-in`; on 200 → returns user; on error → throw typed error.
  - `signOut()`: POST `/api/auth/sign-out`.
  - `getCurrentUser()`: GET `/api/auth/me`.
  - `register(email, password, displayName)`: POST `/api/auth/sign-up` (new route added in phase-10 or here — locked: add to phase-10 if missed).
- `auth-store.ts` (Zustand):
  - State: `user: AuthUser | null; status: 'idle'|'loading'|'authenticated'|'unauthenticated'`.
  - Actions: `signIn(email, password)`, `signOut()`, `hydrate()`, `setUser(user)`.
  - `signIn` calls `HttpAuthClient.signIn` (or stub if flag); updates store; throws on failure for caller to catch.
- `login-form.tsx`: add `password` input (currently only email + displayName stub); use `react-hook-form` + zod for validation; show toast on error.
- `app/(app)/layout.tsx`: on mount, call `useAuthStore.hydrate()` once; show spinner during `status === 'loading'`.
- `app/sw.ts`: prepend Serwist `NetworkOnly` rule for `/api/auth/*`.

**Non-functional**
- All cookies set by route handlers honoured automatically by browser; no manual cookie code in FE.
- TanStack Query (where used for user data) reads from `useAuthStore` — no duplicate fetch.
- `useAuthConfigStore` persisted to localStorage (`lexio:auth-config`); default `'real'` on first visit. Documented in FE README (not `.env.example` — no env var anymore).

## Architecture
```
apps/lexio-web/
├── features/
│   └── auth/
│       ├── store/auth-store.ts                (CHANGED)
│       ├── store/auth-config-store.ts         (NEW — Zustand + persist, { authBackend })
│       ├── components/
│       │   ├── login-form.tsx                 (CHANGED — add password)
│       │   ├── register-form.tsx              (NEW)
│       │   └── auth-debug-panel.tsx           (NEW — dev-only toggle UI)
│       ├── errors.ts                          (NEW — error → toast string)
│       └── hooks/use-hydrate-auth.ts          (NEW)
├── lib/
│   └── api/
│       ├── client.ts                          (CHANGED — extend AuthApi interface)
│       ├── index.ts                           (CHANGED — feature-flag wiring)
│       └── http-auth-client.ts                (NEW — from phase-10 placeholder)
└── app/
    ├── (app)/layout.tsx                       (CHANGED — call hydrate)
    └── sw.ts                                  (CHANGED — Serwist rule)
```

## Related Code Files
**Create:** `register-form.tsx`, `errors.ts`, `use-hydrate-auth.ts`, `http-auth-client.ts`, `auth-config-store.ts`, `auth-debug-panel.tsx`.
**Modify:** `auth-store.ts`, `login-form.tsx`, `client.ts`, `index.ts`, `(app)/layout.tsx`, `sw.ts`, FE README (document `lexio:auth-config` localStorage key).
**Delete:** mock auth-store fallback code (kept behind feature flag — only deleted in Phase 1.5+ once real BE is stable).

## Implementation Steps
1. Extend `AuthApi` interface in `lib/api/client.ts`: `signIn`, `signOut`, `register`, `getCurrentUser`.
2. Implement `HttpAuthClient` calling phase-10 route handlers; throws on non-2xx with typed `AuthError`.
3. Author `features/auth/store/auth-config-store.ts`:
   ```ts
   import { create } from 'zustand';
   import { persist } from 'zustand/middleware';
   type AuthBackend = 'stub' | 'real';
   export const useAuthConfigStore = create(
     persist<{ authBackend: AuthBackend; setAuthBackend: (b: AuthBackend) => void }>(
       (set) => ({
         authBackend: 'real',
         setAuthBackend: (authBackend) => set({ authBackend }),
       }),
       { name: 'lexio:auth-config' },
     ),
   );
   ```
4. Refactor `lib/api/index.ts` to expose a getter (no build-time branching):
   ```ts
   const httpAuthClient = new HttpAuthClient();
   export function getAuthClient() {
     return useAuthConfigStore.getState().authBackend === 'real'
       ? httpAuthClient
       : mockApiClient.auth;
   }
   export const api = { ...mockApiClient, get auth() { return getAuthClient(); } };
   ```
5. Rewrite `auth-store.ts` (Zustand `create`) with the state shape + actions above; replace direct user-object manipulation with `api.auth.signIn(...)` calls (resolves via `getAuthClient()` at call time, so runtime toggle works without re-instantiating the store).
6. Update `login-form.tsx`: add password input + zod schema; on submit → `useAuthStore.signIn(email, password)` → on success navigate to `/decks`; on error toast.
7. Build `register-form.tsx` similar pattern; calls `api.auth.register`.
8. Build `use-hydrate-auth` hook: `useEffect(() => { useAuthStore.getState().hydrate(); }, [])`.
9. Wire hook in `app/(app)/layout.tsx`; render spinner until status leaves `loading`.
10. Update `app/sw.ts`:
    ```ts
    import { registerRoute } from 'serwist';
    registerRoute(({ url }) => url.pathname.startsWith('/api/auth/'), new NetworkOnly());
    // then existing defaultCache rules
    ```
11. Build `auth-debug-panel.tsx` (dev-only):
    ```tsx
    if (process.env.NODE_ENV !== 'development') return null;
    // small fixed-position panel with select: { authBackend, setAuthBackend }
    // also exposes button: clear auth store + TanStack Query cache
    ```
    Mount in `app/(app)/layout.tsx` (or root layout) — Next.js tree-shakes the import out of prod bundles because the body short-circuits.
12. Update FE README: document `lexio:auth-config` localStorage key + how to toggle via debug panel in dev.
13. Manual smoke test full flow: register → auto-login → see /decks → reload (hydrate) → sign-out → redirect.
14. Component tests via Vitest + RTL: login form happy + error paths.
15. E2E (covered in phase-12 but stubbed here): a Playwright test that pre-seeds `localStorage['lexio:auth-config'] = JSON.stringify({ state: { authBackend: 'stub' }, version: 0 })` before navigation, asserts mock client used; second test seeds `'real'`, asserts real client used.
16. PR #2 in FE repo, stacked on phase-10 branch.

## Todo List
- [ ] `HttpAuthClient` implementation
- [ ] `useAuthConfigStore` (Zustand + persist) — `{ authBackend }` runtime flag
- [ ] `getAuthClient()` getter wiring in `lib/api/index.ts` (call-site branching)
- [ ] Zustand `auth-store` rewrite
- [ ] `login-form` + `register-form` with password
- [ ] `use-hydrate-auth` hook + layout wiring
- [ ] Serwist `NetworkOnly` rule
- [ ] `auth-debug-panel.tsx` dev-only toggle (tree-shaken in prod)
- [ ] FE README documents `lexio:auth-config` key
- [ ] Error → toast mapping
- [ ] Vitest unit + RTL component tests green
- [ ] E2E covers both `stub` and `real` modes via pre-seeded localStorage
- [ ] Manual full-flow smoke test passes
- [ ] FE PR #2 opened

## Success Criteria
- Register from FE login form → BE creates user → `audit_logs` shows `UserRegistered` → FE redirects to `/decks` with user populated.
- Reload page → hydrate calls `/api/auth/me` → user still populated; no flash of unauthenticated UI > 200ms.
- Sign-out → cookies cleared → next reload shows `/sign-in`.
- Serwist DevTools confirms `/api/auth/*` requests are NetworkOnly (no service-worker cache hit).
- Setting `localStorage['lexio:auth-config']` to `{state:{authBackend:'stub'}}` and reloading brings back the old Zustand stub end-to-end without rebuild. Flipping back to `'real'` via the dev debug panel restores live BE calls within the same session.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Hydrate flashes unauth UI then auth UI | M | M | Block render in `(app)/layout` until status !== 'loading'; show skeleton. |
| Service worker caches sign-in response with cookie | H | C | NetworkOnly rule MUST register before defaultCache rule — test in Application > Service Workers DevTools. |
| Dexie data orphaned if user signs out + signs in as different user | M | M | On signIn success, if user.id differs from previously-stored id, clear Dexie (already done in some FE; verify + add test). |
| TanStack Query stale-cache leaks user data across sign-out | M | H | On signOut, call `queryClient.clear()`; document pattern. |
| Runtime flag stuck on `'stub'` after dev session, demo to stakeholder hits mocks | M | H | Default is `'real'`; debug panel exists only in dev bundle (`process.env.NODE_ENV === 'development'` short-circuit + Next.js tree-shaking). In production, the flag can only be set to `'stub'` by manually editing localStorage in DevTools — flag this as a known forensic possibility in the FE README; not a real risk for stakeholders. |
| Persisted localStorage flag survives logout / user-switch | L | L | `lexio:auth-config` is per-browser config, not per-user. Acceptable. Clearing site data resets to default `'real'`. |
| Production bundle accidentally includes debug panel | M | M | `NODE_ENV` check at top of component body ensures function returns null; verify via bundle analyzer that `auth-debug-panel.tsx` is either absent or its body is dead-code-eliminated. |

## Security Considerations
- No tokens in localStorage / sessionStorage / Zustand persist — verify via DevTools.
- Logout clears Dexie if user.id changes — prevents cross-user data leak.
- `register-form` enforces same password policy as BE (≥8, ≥1 digit, ≥1 special) via zod; BE remains authoritative.
- Service-worker NetworkOnly rule is the only thing standing between PWA cache + auth requests — covered by integration test.

## Next Steps
Unblocks phase-12 (E2E Playwright runs against both FE + BE together).
