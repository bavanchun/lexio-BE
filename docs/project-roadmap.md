# Project Roadmap

> **Canonical source:** `docs/Lexio_Complete_Documentation.docx` §10 (sprint plan).
> This file tracks BE-specific milestone status only.

## Epic 1 — Foundation Bootstrap ✅ Complete (2026-05-10)

| Phase | Description | Status |
|-------|-------------|--------|
| 01 | Repo init, .gitignore, .editorconfig | ✅ |
| 02 | Directory.Build.props, CPM | ✅ |
| 03 | SharedKernel (DDD primitives + tests) | ✅ |
| 04 | BuildingBlocks.Abstractions + tests | ✅ |
| 05 | BuildingBlocks implementations (6 projects) | ✅ |
| 06 | Docker Compose polyglot stack | ✅ |
| 07 | Secrets + config strategy | ✅ |
| 08 | Lexio.ServiceTemplate (dotnet new) | ✅ |
| 09 | Test infrastructure (TestUtils, arch tests, coverage) | ✅ |
| 10 | Husky pre-commit + commit-msg hooks | ✅ |
| 11 | GitHub Actions CI/CD skeleton | ✅ |
| 12 | Docs skeleton + 5 ADRs + runbooks | ✅ |

## Epic 2 — Identity Service (Next)

- OpenIddict OAuth2 + JWT RS256
- User registration, email verification, password reset
- Profile CRUD
- Integration with Progress service via events

## Epic 3 — Vocabulary Service

- Deck + Card CRUD (MongoDB)
- Rich card content model (front/back/media)
- Search indexing to Elasticsearch

## Epic 4 — Progress Service + SM-2 Engine

- SM-2 scheduling algorithm
- Review session management
- Analytics projections

## Epic 5 — API Gateway + Production Hardening

- YARP reverse proxy
- K8s manifests + Helm charts
- CD pipeline filling out `cd.yml`
- Observability dashboards (Grafana + Jaeger)
