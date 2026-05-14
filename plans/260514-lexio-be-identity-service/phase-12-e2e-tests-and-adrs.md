# Phase 12 — E2E Playwright + docs + ADRs

## Context Links
- Phase-08 integration tests (BE-only)
- Phase-10 + phase-11 FE swap
- `docs/architecture/` (ADR home)
- `docs/system-architecture.md`, `docs/codebase-summary.md`, `docs/project-roadmap.md`

## Overview
- Priority: P1
- Status: pending
- Effort: 3h
- Branches:
  - FE repo: `feat/fe-identity-e2e` (off phase-11)
  - BE repo: `docs/be-identity-adrs` (off phase-08)
- PR: FE PR #3 + BE PR #22

End-to-end Playwright tests exercising the full register → login → me → refresh → logout flow against running compose stack. Three ADRs landing decisions. README updates in both repos. Roadmap + codebase-summary updates.

## Key Insights
- Playwright lives in FE repo (already wired via Next.js conventions); BE runs in compose.
- E2E runner brings up full stack via `docker compose up -d` in CI; Playwright targets `http://localhost:3000`.
- 3 ADRs covering: OpenIddict 6 choice (vs Duende/IdentityServer); refresh-token rotation strategy; FE cookie-proxy pattern (vs SPA-direct).
- Docs sync (per `documentation-management.md` rule): codebase-summary section "Identity Service" added; project-roadmap marks Phase-Identity as ✅ complete with PR list; system-architecture.md adds Identity sequence diagram.

## Requirements
**Functional**
- Playwright suite `apps/lexio-web/e2e/auth.spec.ts`:
  - **Register flow**: navigate `/sign-up` → fill form → submit → assert `/decks` loaded + user name visible.
  - **Login flow**: sign-out → navigate `/sign-in` → submit → assert `/decks`.
  - **Hydrate flow**: reload after login → assert user still authenticated; assert no auth flicker > 300ms.
  - **Logout flow**: click logout → assert `/sign-in`; reload → still `/sign-in`.
  - **Invalid credentials**: submit wrong password → assert error toast + no navigation.
  - **Rate limit**: 6× rapid invalid logins → assert "Too many attempts" toast.
- Playwright config: `webServer` = `pnpm dev`; `globalSetup` = `docker compose up -d identity-api postgres rabbitmq`.

**Docs**
- `docs/architecture/adr-013-identity-openiddict.md` — context, decision (OpenIddict 6 MIT vs Duende paid), consequences.
- `docs/architecture/adr-014-refresh-token-rotation.md` — immediate rotation on issue, FOR UPDATE lock, deferred 30s grace period.
- `docs/architecture/adr-015-fe-cookie-proxy.md` — httpOnly cookie via Next route handler vs SPA-stored token vs BFF.
- `docs/codebase-summary.md` — new section "Identity Service" (project layout, key dependencies, endpoints, events).
- `docs/system-architecture.md` — add Identity service to component diagram + sequence diagram for login flow.
- `docs/project-roadmap.md` — mark "Phase: Identity Microservice" complete; link to PR #13–#22.
- `docs/project-changelog.md` — entry per researcher-04 contract surface.
- README updates in both repos: dev-setup section "Start Identity locally" with copy-paste curl.

## Architecture
```
lexio-app-fe/
└── e2e/
    ├── auth.spec.ts                 (NEW — Playwright)
    ├── fixtures/test-user.ts        (NEW — test data)
    └── playwright.config.ts         (CHANGED — add globalSetup + webServer)

lexio-app-be/docs/
├── architecture/
│   ├── adr-013-identity-openiddict.md       (NEW)
│   ├── adr-014-refresh-token-rotation.md    (NEW)
│   └── adr-015-fe-cookie-proxy.md           (NEW)
├── codebase-summary.md                       (CHANGED)
├── system-architecture.md                    (CHANGED)
├── project-roadmap.md                        (CHANGED)
└── project-changelog.md                      (CHANGED)
```

## Related Code Files
**Create:**
- FE: `e2e/auth.spec.ts`, `e2e/fixtures/test-user.ts`, `e2e/utils/reset-db.ts` (calls BE admin endpoint or direct psql to wipe Testcontainers).
- BE: 3 ADR files.

**Modify:**
- FE: `playwright.config.ts`, `package.json` (e2e scripts), `README.md`.
- BE: 4 docs files above, `README.md`.

## Implementation Steps
1. Install Playwright in FE repo if not already: `pnpm dlx playwright install`.
2. Write `playwright.config.ts`:
   - `webServer: { command: 'pnpm dev', port: 3000, reuseExistingServer: !process.env.CI }`.
   - `globalSetup: './e2e/global-setup.ts'` — runs `docker compose -f ../lexio-app-be/docker-compose.yml up -d identity-api postgres rabbitmq` and waits for `/healthz`.
   - `globalTeardown` — optional; skip if reusing locally.
3. Write `e2e/utils/reset-db.ts`: connects to Postgres via `pg` lib + truncates `users`, `refresh_tokens`, `audit_logs`, `outbox_messages` (NOT roles). Run before each test via `beforeEach`.
4. Write `auth.spec.ts` covering 6 scenarios above.
5. CI integration: `.github/workflows/e2e.yml` (FE repo) — checkout FE + BE, `docker compose up`, `pnpm test:e2e`. Run on PRs touching auth files only (path filter).
6. Write 3 ADRs (in BE repo `docs/architecture/`). Each ADR:
   - Title, status (Accepted), date.
   - Context (problem).
   - Decision (chosen option + rationale).
   - Alternatives considered (briefly).
   - Consequences (positive + negative + migration).
7. Update `codebase-summary.md`: insert "Identity Service" section with diagram + endpoint list + key deps.
8. Update `system-architecture.md`: insert Mermaid sequence diagram for register/login.
9. Update `project-roadmap.md`: tick Identity phase complete; reference PR #13–#22.
10. Update `project-changelog.md` with entry: "feat(identity): launch Identity microservice (8 endpoints, OpenIddict 6, RBAC, outbox)".
11. Update FE README dev-setup section.
12. Open 2 PRs (BE docs, FE e2e). After merge, close out the Identity-phase tracking.

## Todo List
- [ ] Playwright config + global-setup + reset-db util
- [ ] 6 E2E test cases passing locally
- [ ] CI workflow added (FE repo)
- [ ] ADR-013 OpenIddict
- [ ] ADR-014 Refresh rotation
- [ ] ADR-015 FE cookie proxy
- [ ] codebase-summary updated
- [ ] system-architecture diagrams updated
- [ ] project-roadmap marked complete
- [ ] project-changelog entry
- [ ] Both READMEs updated
- [ ] FE PR + BE PR opened, both green in CI

## Success Criteria
- `pnpm test:e2e` green locally + in CI.
- All 6 scenarios assert visible UX state (not just HTTP status).
- `docs/architecture/` contains 3 new ADR files; each cross-linked from `system-architecture.md`.
- `docs/project-roadmap.md` shows Identity phase ✅ with effort + PR-range citation.
- Onboarding dev runs README copy-paste → has working register/login in < 5 min.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Playwright flaky on CI due to compose startup time | H | M | `globalSetup` polls `/healthz` with 60s timeout; `webServer.timeout` raised to 120s; retry 2× on failure. |
| Cross-repo CI orchestration (FE pulls BE compose) | M | M | Pin BE submodule commit OR use `actions/checkout` of BE repo with explicit ref; document in workflow. |
| ADRs accumulate stale references after rename/refactor | M | L | Per `review-audit-self-decision.md` rule 5, ADRs reference stable concepts; avoid phase numbers in body. |
| Docs drift after future phases | M | M | `docs-manager` agent auto-runs on PR merge (per `documentation-management.md`); ADRs immutable, only newer ADRs supersede. |

## Security Considerations
- E2E test users created with throwaway passwords (`E2eTest!1`) — never reused for staging/prod.
- Reset-db script restricted to test DB connection string; refuses to run if `LEXIO_ENV !== 'test'`.
- ADR-014 documents key-compromise response procedure (cross-references phase-09 runbook).

## Next Steps
After phase-12 merge, Identity microservice is feature-complete. Subsequent vertical-slice services (Decks, Cards, Statistics) reuse this plan structure. Phase 1.5 features (email verification, password reset, OAuth Google callback) tracked separately in `docs/project-roadmap.md` next iteration.
