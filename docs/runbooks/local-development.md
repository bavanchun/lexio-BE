# Local Development Runbook

> Skeleton — expanded in phase 12.

## Prerequisites

- .NET 10 SDK at `$HOME/.dotnet` (verify: `dotnet --version`)
- Docker Desktop ≥ 4.x (or OrbStack on macOS)
- gh CLI authenticated (`gh auth status`)
- Node.js ≥ 20 (for commitlint pre-commit hook)

## First-Time Setup

```bash
git clone git@github.com:bavanchun/lexio-BE.git && cd lexio-BE
cp .env.example .env
npm install                        # commitlint devDeps
dotnet tool restore                # Husky + dotnet-ef + dotnet-outdated
docker compose up -d               # Start polyglot stack
dotnet restore && dotnet build && dotnet test
```

## Running a Service Locally

> Expanded in phase 12 once the service template exists.

## Connecting to the Compose Stack

| Service | URL / host | Port |
|---------|-----------|------|
| Postgres | `localhost` | `5432` |
| MongoDB | `localhost` | `27017` |
| Redis | `localhost` | `6379` |
| RabbitMQ (AMQP) | `localhost` | `5672` |
| RabbitMQ (UI) | http://localhost:15672 | — |
| Kafka | `localhost` | `9092` |
| Elasticsearch | http://localhost:9200 | — |

Default dev credentials are in `.env.example`.
