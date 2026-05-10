# 0004. Event-Driven with RabbitMQ + Kafka Hybrid

Date: 2026-05-10

## Status

Accepted

## Context

Lexio services need to communicate asynchronously for:
1. **Integration events** — e.g., `UserRegistered` triggers welcome email; `ReviewScheduled` triggers
   notification. These are durable, at-least-once, relatively low volume.
2. **Analytics / event streams** — e.g., raw SM-2 review outcomes fed into ML pipeline for
   model retraining. High throughput, log-compacted, replay-able.

Options evaluated:
- **RabbitMQ only** — proven, simple DLQ/retry model, good MassTransit integration; weak at
  long-term log retention and high-throughput stream processing.
- **Kafka only** — excellent for streams; operational complexity high for simple work-queues;
  delayed messaging requires 3rd-party plugin or workaround.
- **Hybrid** — use each broker for what it excels at.

## Decision

Use a **hybrid broker strategy**:

| Concern | Broker | Library |
|---------|--------|---------|
| Integration events (service-to-service commands/notifications) | RabbitMQ 3.13 + `rabbitmq_delayed_message_exchange` | MassTransit 8 |
| Analytics streams (ML pipeline, audit log, event sourcing) | Apache Kafka 3.7 (KRaft mode) | Confluent.Kafka |

`IEventBus` abstraction in `Lexio.BuildingBlocks.Abstractions` decouples business code from
the transport. `MassTransitEventBus` (internal) implements it via MassTransit's `IPublishEndpoint`.

Outbox pattern (`OutboxMessageEntity` + `LexioDbContextBase`) ensures at-least-once delivery
of domain events even if the broker is temporarily unavailable.

## Consequences

**Positive:**
- Each broker used for its strengths; neither over-engineered.
- MassTransit abstracts RabbitMQ topology (exchanges, queues, retry policies).
- Kafka KRaft mode (no ZooKeeper) simplifies local compose setup.

**Negative:**
- Two brokers to operate and monitor in production.
- Developers must understand which events go where.

**Neutral:**
- `docker-compose.yml` runs both brokers locally for development parity.
- Outbox relay worker (to be implemented per-service) dispatches `OutboxMessageEntity` rows
  to the appropriate broker after DB commit.
