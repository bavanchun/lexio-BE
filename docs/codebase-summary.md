# Codebase Summary

> Last updated: 2026-05-10 (foundation bootstrap complete, phases 01–12).

## Repository Layout

```
src/shared/
  Lexio.SharedKernel               DDD primitives: Entity<TId>, AggregateRoot<TId>,
                                   ValueObject, IDomainEvent, Result<T>, Maybe<T>, Error, IClock
  Lexio.BuildingBlocks.Abstractions  Interfaces: IEventBus, IOutbox, ILexioCache,
                                     ICurrentUserAccessor, IUnitOfWork, OutboxMessage
  Lexio.BuildingBlocks.Caching     RedisLexioCache (StackExchange.Redis)
  Lexio.BuildingBlocks.Messaging   MassTransitEventBus + KafkaEventPublisher
  Lexio.BuildingBlocks.Observability  SystemClock, LexioActivitySource, OTel DI
  Lexio.BuildingBlocks.Persistence LexioDbContextBase (audit, soft-delete, outbox)
  Lexio.BuildingBlocks.Auth        JWT RS256 bearer, CurrentUserAccessor, policies
  Lexio.BuildingBlocks.Web         Exception middleware, CorrelationId, ResultExtensions

src/services/                      (empty — services land here in next epic)

templates/
  Lexio.ServiceTemplate            dotnet new lexio-service scaffold

tests/
  _shared/Lexio.TestUtils          Testcontainer fixtures + TestClock
  architecture/Lexio.Architecture.Tests  Repo-wide NetArchTest
  shared/Lexio.SharedKernel.Tests  27 unit tests
  shared/Lexio.BuildingBlocks.Abstractions.Tests  10 contract tests
```

## Key Numbers (foundation)

| Metric | Value |
|--------|-------|
| Projects | 12 src + 3 test |
| Tests | 53 (all green) |
| Build warnings | 0 |
| Known CVEs (direct) | 0 |

## Entry Points

- Start infrastructure: `docker compose up -d`
- Build: `dotnet build`
- Test: `dotnet test`
- New service: `bash scripts/new-service.sh <Name>`
- Coverage report: `bash scripts/coverage.sh --open`
