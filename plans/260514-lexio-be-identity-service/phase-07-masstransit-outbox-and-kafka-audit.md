# Phase 07 — MassTransit publishers (outbox) + Kafka audit-log producer

## Context Links
- researcher-04 §5 (audit events), §6 (domain events + outbox pattern)
- BuildingBlocks: `Lexio.BuildingBlocks.Messaging` — MassTransit + outbox helpers, Kafka producer abstraction
- Foundation phase-05 — RabbitMQ + Kafka local compose

## Overview
- Priority: P1
- Status: pending
- Effort: 3h
- Branch: `feat/be-identity-messaging` (off phase-06)
- PR: stacked PR #19

Wire domain events → outbox → MassTransit → RabbitMQ (5 integration events). Wire audit events → Kafka topic `vocab.audit-log` (11 event types). Define shared contract types in new `Lexio.Identity.Contracts` project (consumable by other services).

## Key Insights
- Outbox already configured by `LexioDbContextBase`; this phase adds the MassTransit EF outbox plugin (`AddEntityFrameworkOutbox<IdentityDbContext>`) plus message consumers/publishers wiring.
- Integration events live in a separate **Contracts** project — never reference Domain types (Guids/strings only) so other services consume without dragging Identity internals.
- Audit logger writes to BOTH the `audit_logs` Postgres table (transactional, queryable) AND Kafka (streaming consumers). The Postgres write is part of the request transaction; the Kafka publish goes through outbox to guarantee at-least-once.
- Domain events → integration events via outbox mapper (`UserRegisteredDomainEvent` → `UserRegisteredEvent` contract).

## Requirements
**Functional**
- New project `Lexio.Identity.Contracts` containing 5 integration event records:
  - `UserRegisteredEvent(UserId, Email, DisplayName, Role, RegisteredAt, Provider)`
  - `UserLoggedInEvent(UserId, LoginAt, IpAddress)`
  - `PasswordChangedEvent(UserId, ChangedAt)`
  - `RoleChangedEvent(UserId, OldRole, NewRole, ChangedByAdminId, ChangedAt)`
  - `UserBannedEvent(UserId, Reason, BannedByAdminId, BannedAt)`
- Outbox dispatcher publishes integration events to RabbitMQ.
- `audit_logs` table created (new migration) per researcher-04 §5.2.
- `AuditLogger : IAuditLogger` implementation writes to `audit_logs` table + enqueues Kafka message via outbox.
- Kafka topic `vocab.audit-log` produced via `IBuildingBlocksKafkaProducer` (foundation).

**Non-functional**
- Outbox dispatcher runs as `IHostedService` (already provided by MassTransit).
- Messages serialised JSON; consumer compatibility via additive-only changes (no field renames).
- Correlation ID propagated from inbound HTTP request (W3C TraceContext) into outbox message header.

## Architecture
```
src/services/Lexio.Identity.Contracts/             (NEW project)
└── IntegrationEvents/
    ├── UserRegisteredEvent.cs
    ├── UserLoggedInEvent.cs
    ├── PasswordChangedEvent.cs
    ├── RoleChangedEvent.cs
    └── UserBannedEvent.cs

Lexio.Identity.Infrastructure/
├── Messaging/
│   ├── DomainToIntegrationEventMapper.cs   (Domain → Contracts)
│   ├── OutboxIntegrationEventDispatcher.cs (hooked into LexioDbContextBase.CollectOutboxMessages)
│   └── MassTransitRegistrationExtensions.cs (AddIdentityMessaging)
├── Auditing/
│   ├── AuditLogger.cs                       (IAuditLogger impl)
│   └── AuditLogEntryConfiguration.cs        (EF config)
└── Persistence/Migrations/
    └── 20260525_AddAuditLogs.cs             (generated)
```

## Related Code Files
**Create:** new Contracts project (csproj + 5 events) + 4 cs files in Infrastructure + migration.
**Modify:**
- `Lexio.slnx` adds Contracts project.
- `Lexio.Identity.Infrastructure.csproj` references Contracts.
- `Lexio.Identity.Api/Program.cs` calls `AddIdentityMessaging()`.
- `Directory.Packages.props` confirms `MassTransit.EntityFrameworkCore` pinned.

## Implementation Steps
1. Create `Lexio.Identity.Contracts` class library; add 5 record events; add to slnx.
2. Implement `DomainToIntegrationEventMapper`: switch-on type from Domain event → Contracts event (Guid casts).
3. In Domain event raising sites (phase-02 `User` aggregate), the events stored on aggregate are Domain types; `LexioDbContextBase.CollectOutboxMessages` already serialises them. Add hook so dispatcher maps Domain → Integration via the mapper before MassTransit publish.
4. `MassTransitRegistrationExtensions.AddIdentityMessaging`:
   ```
   services.AddMassTransit(cfg => {
     cfg.AddEntityFrameworkOutbox<IdentityDbContext>(o => {
       o.UsePostgres();
       o.UseBusOutbox();
       o.QueryDelay = TimeSpan.FromSeconds(1);
     });
     cfg.UsingRabbitMq((ctx, bus) => {
       bus.Host(config["RabbitMq:Host"], h => { h.Username(...); h.Password(...); });
       bus.ConfigureEndpoints(ctx);
     });
   });
   ```
5. Add `audit_logs` entity + configuration + migration `AddAuditLogs`.
6. Implement `AuditLogger`:
   - Constructor injects `IdentityDbContext` + Kafka producer.
   - `LogAsync(eventType, userId, adminId?, ip, userAgent, payload)`:
     - Insert row into `audit_logs` (same transaction as caller's UoW).
     - Add outbox message for Kafka topic via `IBus.Publish` (MassTransit will route to Kafka producer registered in BuildingBlocks).
7. Update all 8 handlers (phase-03) to call `IAuditLogger.LogAsync` for the 11 event types per researcher-04 §5.1.
8. Integration test (Testcontainers Postgres + RabbitMQ + Kafka via containers): register a user → assert `outbox_messages` row → assert RabbitMQ consumer receives `UserRegisteredEvent` → assert Kafka `vocab.audit-log` topic receives audit message.
9. PR #19 stacked on phase-06.

## Todo List
- [ ] New Contracts project + 5 events
- [ ] DomainToIntegrationEventMapper
- [ ] MassTransit + EF outbox wiring
- [ ] `audit_logs` migration
- [ ] `AuditLogger` impl
- [ ] 11 audit-event call sites wired in handlers
- [ ] Integration test green (Postgres + RMQ + Kafka)
- [ ] PR #19 opened

## Success Criteria
- After `POST /auth/register`, `SELECT * FROM outbox_messages` shows 1 unprocessed row, then 0 within 5s.
- RabbitMQ exchange `Lexio.Identity.Contracts:UserRegisteredEvent` has 1 message visible in management UI.
- Kafka topic `vocab.audit-log` has 1 message visible via `kafka-console-consumer`.
- `audit_logs` table has 1 row with `event_type='UserRegistered'`.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Outbox dispatcher hot-loop on poison message | M | H | MassTransit retry policy: 3 attempts → DLQ (`Lexio.Identity-error` queue). |
| Domain event references domain types in payload (breaks contract decoupling) | M | H | Mapper enforced at compile time; Contracts events contain only primitives (Guid/string/DateTimeOffset). |
| Kafka topic missing on first run | M | M | Topic auto-create in dev; explicit creation script in `infra/kafka/init-topics.sh` for staging/prod. |
| PII leak in Kafka audit messages | M | H | `payload` JSONB excludes raw email; uses `user_id` only. Researcher-04 §5 confirmed. |
| Transaction-spanning RMQ publish | M | H | `UseBusOutbox` ensures publish happens after commit, not before — researcher-02 §6 pattern. |

## Security Considerations
- RabbitMQ + Kafka credentials from env, never committed.
- Audit log payload schema versioned via `payload.schemaVersion` field; consumers tolerate additive changes only.
- DLQ contents may contain PII — restrict access in production (documented in phase-09 runbook).

## Next Steps
Unblocks phase-08 (test suite covers messaging end-to-end) and phase-09 (compose pulls full stack online).
