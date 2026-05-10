# System Architecture

> **Canonical source:** `docs/Lexio_Complete_Documentation.docx` §7.
> This file is a high-level BE reference. See ADRs for decision rationale.

## Service Map

```
                        ┌─────────────┐
  Next.js 15 PWA ──────▶│  API Gateway │  (YARP — phase 3)
                        └──────┬──────┘
              ┌────────────────┼──────────────────┐
              ▼                ▼                  ▼
       ┌──────────┐    ┌──────────────┐   ┌────────────┐
       │ Identity │    │  Vocabulary  │   │  Progress  │
       │ (PG 17)  │    │  (Mongo 8)   │   │  (PG 17)   │
       └────┬─────┘    └──────┬───────┘   └─────┬──────┘
            │                 │                  │
            └─────────────────▼──────────────────┘
                         RabbitMQ 3.13
                       (integration events)
                              │
                    ┌─────────▼──────────┐
                    │  Kafka 3.7 (KRaft) │  ← analytics / ML streams
                    └────────────────────┘
```

## Cross-Cutting Infrastructure

| Concern | Technology |
|---------|-----------|
| Cache | Redis 7 |
| Search index | Elasticsearch 8 |
| Observability | OpenTelemetry → OTLP collector |
| Logging | Serilog → OpenTelemetry sink |
| Auth tokens | OpenIddict (Identity service), JWT RS256 |
| Outbox | `OutboxMessageEntity` rows in each service's DB |

## Per-Service Internal Architecture

See [ADR 0002](architecture/0002-clean-architecture-per-service.md) — Clean Architecture layers.

## Decisions

- Broker choice: [ADR 0004](architecture/0004-event-driven-rabbitmq-kafka-hybrid.md)
- Database strategy: [ADR 0005](architecture/0005-database-per-service-polyglot.md)
