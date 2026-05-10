# Phase 12 — Docs skeleton + ADR set + runbooks

## Context Links
- All prior phases (this is the documentation crystallisation)
- Project rule: `./docs` structure (project-overview-pdr, code-standards, etc.)

## Overview
- Priority: P2
- Status: pending
- Brief: Stand up `/docs` with README, 5 ADRs (MADR), runbooks, and per-service OpenAPI placeholder.

## Key Insights
- ADRs use MADR (Markdown ADR) template: Status / Context / Decision / Consequences.
- `docs/` mirrors structure required by project CLAUDE.md (project-overview-pdr.md, code-standards.md, codebase-summary.md, design-guidelines.md, deployment-guide.md, system-architecture.md, project-roadmap.md). Keep these as skeleton-only references back to source-of-truth docx; don't duplicate the docx.
- README must include CI badges + quick-start.

## Requirements
- Functional:
  - Root `README.md`
  - `docs/architecture/` with 5 ADRs
  - `docs/runbooks/` (configuration.md from phase 07; expand local-development.md, troubleshooting.md)
  - `docs/api/` placeholder
  - Skeleton: `docs/{project-overview-pdr,code-standards,codebase-summary,design-guidelines,deployment-guide,system-architecture,project-roadmap}.md` (each ≤80 lines, links to source-of-truth docx for canonical detail).
- NFR: a new dev clones repo + runs `docker compose up && dotnet build && dotnet test` from README alone.

## Architecture
```
README.md
docs/
├── architecture/
│   ├── 0001-monorepo-with-dotnet-10.md
│   ├── 0002-clean-architecture-per-service.md
│   ├── 0003-cqrs-with-mediator-not-mediatr.md
│   ├── 0004-event-driven-with-rabbitmq-and-kafka.md
│   ├── 0005-database-per-service-polyglot.md
│   └── README.md           (ADR index)
├── api/.gitkeep
├── runbooks/
│   ├── configuration.md     (phase 07)
│   ├── local-development.md
│   └── troubleshooting.md
├── project-overview-pdr.md
├── code-standards.md
├── codebase-summary.md
├── design-guidelines.md
├── deployment-guide.md
├── system-architecture.md
└── project-roadmap.md
```

## Related Code Files
Create all files above.

## Implementation Steps
1. Branch `docs/be-foundation-docs` off `feat/be-ci`.
2. Root `README.md` template:
   - Title, badges (CI status), one-paragraph description.
   - Prerequisites: .NET 10 SDK, Docker, gh CLI, Node ≥20.
   - Quick start:
     ```
     git clone git@github.com:bavanchun/lexio-BE.git && cd lexio-BE
     cp .env.example .env
     docker compose up -d
     npm install            # commitlint deps
     dotnet tool restore
     dotnet restore && dotnet build && dotnet test
     ```
   - Project structure tree.
   - Adding a new service: `bash scripts/new-service.sh Identity`.
   - Contributing: Conventional Commits, stacked PR protocol (link to FE convention), branch naming.
   - Links to ADRs.
3. ADR template (MADR):
   ```markdown
   # NNNN. Title
   Date: 2026-05-10
   ## Status
   Accepted
   ## Context
   ...
   ## Decision
   ...
   ## Consequences
   Positive: ...
   Negative: ...
   Neutral: ...
   ```
4. ADR contents:
   - **0001 Monorepo with .NET 10**: rationale (single sln, CI simplicity, refactor velocity); rejected alternatives (multi-repo per service).
   - **0002 Clean Architecture per service**: doc §6.7 baseline; layer rules; NetArchTest enforcement.
   - **0003 CQRS with Mediator (not MediatR)**: licensing rationale (MediatR commercial v13+); martinothamar/Mediator chosen for MIT + source-gen perf.
   - **0004 Event-driven with RabbitMQ + Kafka hybrid**: RabbitMQ for command-style integration events (work queues, delayed); Kafka for analytics/event-sourcing streams.
   - **0005 Database per service (polyglot)**: doc §7.1; Postgres for relational; Mongo for card content; Redis for cache; ES for search.
5. `runbooks/local-development.md`: complete from phase 07 skeleton — every step from clone to running first service.
6. `runbooks/troubleshooting.md`: common issues (kafka cluster id mismatch, port collisions, ES memlock on macOS, dotnet SDK version mismatch, husky on Windows, dotnet format warning-as-error tweaks).
7. `docs/{project-overview-pdr,system-architecture,...}.md`: 1-page each, links to source-of-truth docx section numbers + brief BE-only excerpt + link to relevant ADR.
8. `docs/architecture/README.md` ADR index.
9. Commit: `docs(be-docs): add README, ADRs, runbooks`.

## Todo List
- [ ] Root `README.md` complete with badges + quick start
- [ ] 5 ADRs written in MADR format
- [ ] ADR index `docs/architecture/README.md`
- [ ] `local-development.md` complete
- [ ] `troubleshooting.md` covers compose, dotnet, husky issues
- [ ] 7 top-level docs/ skeletons each linking to source-of-truth
- [ ] PR opened + CI green

## Success Criteria
- Cold-clone test: a teammate follows README and lands at green `dotnet test` in <30 min.
- ADRs renderable on GitHub.
- All internal links resolve (run `lychee` or `markdown-link-check` locally).

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Docs drift from code | H | M | ADR-only for decisions; runbooks reviewed each release; pin docx version in skeletons |
| README quick-start fails on Windows | M | M | Document WSL2 as supported env; PowerShell variants for `cp`/`bash` |
| ADR `Accepted` status outdated when decisions change | M | L | Process: any future change creates new ADR with `Supersedes 000X`; old ADR becomes `Superseded` |

## Security Considerations
- Don't include secrets, internal URLs, or proprietary specs in docs/.
- Link source-of-truth docx but do NOT inline its content (avoid divergence).

## Next Steps
Foundation bootstrap COMPLETE after this phase. Next epic (out of scope here):
- Service implementation phase (Identity first, per doc §10.x sprint plan).
- YARP API gateway.
- K8s manifests + Helm charts.
- CD pipeline filling out `cd.yml`.
