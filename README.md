# Lexio BE

[![ci](https://github.com/bavanchun/lexio-BE/actions/workflows/ci.yml/badge.svg)](https://github.com/bavanchun/lexio-BE/actions/workflows/ci.yml)

Backend monorepo for the Lexio vocabulary platform — .NET 10 microservices with clean architecture, DDD, and event-driven messaging.

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0.x |
| Docker + Compose | latest stable |
| Node.js | ≥ 20 |
| GitHub CLI (`gh`) | latest |

## Quick Start

```bash
git clone git@github.com:bavanchun/lexio-BE.git && cd lexio-BE

# 1. Infrastructure (Postgres, Mongo, Redis, RabbitMQ, Kafka, Elasticsearch)
docker compose up -d

# 2. Node tooling (commitlint for commit-msg hooks)
npm install

# 3. .NET tooling (Husky, EF tools, ReportGenerator)
dotnet tool restore

# 4. Build and test
dotnet restore && dotnet build && dotnet test
```

> **Windows**: Use WSL2. `bash scripts/` utilities require a POSIX shell.

## Project Structure

```
lexio-BE/
├── src/
│   ├── shared/
│   │   ├── Lexio.SharedKernel/          # DDD primitives (Entity, AggregateRoot, Result, Maybe)
│   │   ├── Lexio.BuildingBlocks.*       # Cross-cutting: Abstractions, Auth, Caching, Messaging, Observability, Persistence, Web
│   └── services/                        # Service implementations land here (Identity, Vocabulary, …)
├── templates/
│   └── Lexio.ServiceTemplate/           # dotnet new lexio-service scaffold
├── tests/
│   ├── _shared/Lexio.TestUtils/         # Testcontainer fixtures + TestClock
│   ├── architecture/                    # Repo-wide NetArchTest invariants
│   └── shared/                          # Per-building-block unit tests
├── scripts/
│   ├── new-service.sh                   # Scaffold + sln-add a new service
│   └── coverage.sh                      # Full coverage report (HTML + text)
├── infra/                               # Docker init scripts, RabbitMQ Dockerfile
└── docs/                                # ADRs, runbooks, project skeleton docs
```

## Adding a New Service

```bash
bash scripts/new-service.sh Identity
# Generates src/services/Identity/ with Domain/Application/Infrastructure/Api layers
# Adds all projects to Lexio.slnx automatically
```

## Contributing

- **Branch naming**: `feat/be-{area}`, `fix/be-{issue}`, `docs/be-{topic}`
- **Commits**: [Conventional Commits](https://www.conventionalcommits.org/) enforced by Husky commit-msg hook
  - Allowed scopes: `be-repo`, `be-build`, `be-shared`, `be-infra`, `be-config`, `be-template`, `be-test`, `be-husky`, `be-ci`, `be-docs`, `be-{service-name}`
- **PRs**: Stacked on feature branch of the previous phase/feature; set PR base accordingly
- **Hooks bypass**: Never use `--no-verify` in normal workflow — CI will catch violations

## Architecture Decision Records

See [`docs/architecture/`](docs/architecture/README.md) for all ADRs.

## Documentation

| Doc | Contents |
|-----|----------|
| [`docs/system-architecture.md`](docs/system-architecture.md) | High-level service map |
| [`docs/code-standards.md`](docs/code-standards.md) | Coding conventions |
| [`docs/runbooks/local-development.md`](docs/runbooks/local-development.md) | Full local setup guide |
| [`docs/runbooks/configuration.md`](docs/runbooks/configuration.md) | Config layers and secrets |
| [`docs/runbooks/troubleshooting.md`](docs/runbooks/troubleshooting.md) | Common issues |
