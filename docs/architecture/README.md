# Architecture Decision Records

All architectural decisions for Lexio BE are recorded here using [MADR](https://adr.github.io/madr/) format.

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-monorepo-with-dotnet-10.md) | Monorepo with .NET 10 | Accepted |
| [0002](0002-clean-architecture-per-service.md) | Clean Architecture per Service | Accepted |
| [0003](0003-cqrs-with-mediator-not-mediatr.md) | CQRS with Mediator (not MediatR) | Accepted |
| [0004](0004-event-driven-rabbitmq-kafka-hybrid.md) | Event-Driven with RabbitMQ + Kafka Hybrid | Accepted |
| [0005](0005-database-per-service-polyglot.md) | Database per Service (Polyglot Persistence) | Accepted |

## Process

- New decisions: create `NNNN-short-title.md` using the MADR template.
- Superseded decisions: set status to `Superseded by [MMMM](MMMM-...)` and create the new ADR.
- Do not delete old ADRs — history is preserved.
