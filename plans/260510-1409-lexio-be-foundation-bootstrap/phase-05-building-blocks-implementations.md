# Phase 05 — BuildingBlocks implementation projects

## Context Links
- Doc §6.7.3 (Lexio.Identity.Infrastructure structure as reference)
- Doc §9.1 stack manifest
- Phase 04 abstractions (must already exist)
- Researcher A (Mediator/MassTransit licensing); Researcher B (broker config)

## Overview
- Priority: P1
- Status: pending
- Brief: Concrete cross-cutting infrastructure that each service Infrastructure layer pulls in.

## Key Insights
- This phase is the FATTEST. Consider further sub-phases if PR review > 600 LOC.
- Dependency Injection: each project exposes `Add{X}()` extension on `IServiceCollection` so service Program.cs is one-liner per concern.
- Outbox tied to EF Core — keep its EF dependency in `Lexio.BuildingBlocks.Persistence`, not in `.Abstractions`.

## Requirements
Six projects:
1. **Lexio.BuildingBlocks.Caching** — `LexioCache` impl over `StackExchange.Redis` IConnectionMultiplexer; JSON serialization (System.Text.Json).
2. **Lexio.BuildingBlocks.Messaging** — `MassTransitEventBus : IEventBus` + Kafka publisher wrapper. `AddLexioMessaging(IConfiguration)` extension wires RabbitMQ + Kafka producer based on appsettings.
3. **Lexio.BuildingBlocks.Observability** — `AddLexioObservability()` registers Serilog + OpenTelemetry traces/metrics/logs to OTLP endpoint (env var `OTEL_EXPORTER_OTLP_ENDPOINT`). Includes `ActivitySource` factory and standard log enrichers (correlation id, user id).
4. **Lexio.BuildingBlocks.Persistence** — `LexioDbContextBase : DbContext` with audit fields (`CreatedAt`, `UpdatedAt`, `CreatedBy`), soft-delete query filter (`IsDeleted`), Outbox EF entity config, `SaveChangesAsync` override that dispatches domain events + persists outbox messages atomically. Implements `IUnitOfWork`.
5. **Lexio.BuildingBlocks.Web** — middleware: `ProblemDetails` exception handler (RFC 7807, maps `Result.Error` → ProblemDetails), `CorrelationIdMiddleware`, request logging via Serilog, global exception filter. `AddLexioWeb()` extension.
6. **Lexio.BuildingBlocks.Auth** — JWT bearer config with RS256 (config from `Jwt:` section), `CurrentUserAccessor : ICurrentUserAccessor` reading `HttpContext.User`, claims helpers. `AddLexioAuth(IConfiguration)`.

## Architecture
```
src/shared/
├── Lexio.BuildingBlocks.Abstractions/  (phase 04)
├── Lexio.BuildingBlocks.Caching/
├── Lexio.BuildingBlocks.Messaging/
├── Lexio.BuildingBlocks.Observability/
├── Lexio.BuildingBlocks.Persistence/   (refs EF Core)
├── Lexio.BuildingBlocks.Web/           (refs AspNetCore)
└── Lexio.BuildingBlocks.Auth/          (refs AspNetCore + JwtBearer)
```

Project refs: each → Abstractions + SharedKernel + relevant NuGets. None reference each other except Web → Abstractions.

## Related Code Files
Create one csproj + one `DependencyInjection.cs` extension + impl classes per project. Each project ≤8 files. Tests per project under `tests/shared/...Tests/`.

## Implementation Steps
1. Branch `feat/be-bb-impls` off `feat/be-bb-abstractions`.
2. **Decision gate**: confirm MassTransit 8.x final pin in `Directory.Packages.props` (resolves Unresolved Q1).
3. Create 6 projects under `src/shared/`. Add to sln.
4. `Lexio.BuildingBlocks.Caching`:
   - Add `StackExchange.Redis` ref. Implement `RedisLexioCache : ILexioCache` with `GetAsync<T>`, `SetAsync<T>(key, value, ttl)`, `RemoveAsync`. Use System.Text.Json.
   - `AddLexioCaching(IConfiguration)` reads `Redis:ConnectionString`, registers singleton `IConnectionMultiplexer`.
5. `Lexio.BuildingBlocks.Messaging`:
   - Add `MassTransit`, `MassTransit.RabbitMQ`, `Confluent.Kafka` refs.
   - `MassTransitEventBus : IEventBus` wraps `IPublishEndpoint`.
   - `KafkaEventPublisher` thin wrapper around `IProducer<,>`.
   - `AddLexioMessaging(IConfiguration)`: `services.AddMassTransit(x => x.UsingRabbitMq((ctx, cfg) => cfg.Host(...)))` + Kafka producer factory.
6. `Lexio.BuildingBlocks.Observability`:
   - Add Serilog packages + OpenTelemetry packages (per Directory.Packages.props).
   - `AddLexioObservability(string serviceName)` — configures Serilog (console + OTLP sink), OpenTelemetry traces (AspNetCore + Http instrumentation), metrics, OTLP exporter.
   - `LexioActivitySource` static helper.
7. `Lexio.BuildingBlocks.Persistence`:
   - Add EF Core packages.
   - `LexioDbContextBase : DbContext, IUnitOfWork`. Override `SaveChangesAsync`: stamp audit fields via `ChangeTracker`, collect `IDomainEvent` from `AggregateRoot` entities, persist as `OutboxMessage` rows, then base SaveChanges in a single transaction.
   - `OutboxMessageEntityTypeConfiguration` — table `outbox_messages`.
   - Soft-delete query filter helper extension `entity.HasQueryFilter(e => !e.IsDeleted)` applied via convention.
8. `Lexio.BuildingBlocks.Web`:
   - Refs `Microsoft.AspNetCore.App` framework reference (`<FrameworkReference Include="Microsoft.AspNetCore.App" />`).
   - `LexioExceptionHandlingMiddleware` — catches unhandled, maps to ProblemDetails. Maps `Result` failure error types to HTTP status codes (Validation→400, NotFound→404, Conflict→409, Unauthorized→401, Forbidden→403, Internal→500).
   - `CorrelationIdMiddleware` — reads `X-Correlation-Id` header or generates Guid.
   - `AddLexioWeb()` extension.
9. `Lexio.BuildingBlocks.Auth`:
   - `<FrameworkReference Include="Microsoft.AspNetCore.App" />` + `Microsoft.AspNetCore.Authentication.JwtBearer`.
   - `AddLexioAuth(IConfiguration)` — JwtBearer with RS256, public key from `Jwt:PublicKey` config.
   - `CurrentUserAccessor : ICurrentUserAccessor` reads `IHttpContextAccessor`.
   - `AuthorizationPolicies` — common policy names (e.g. `RequireUser`, `RequireAdmin`).
10. Per-project test project under `tests/shared/...Tests/`. Use Moq for non-IO; defer Testcontainer-backed integration tests to phase 09.
11. `dotnet build` + `dotnet test` clean.
12. Commit per sub-project: `feat(be-shared): add BuildingBlocks.Caching impl` etc. (6 commits in this phase, one PR or split into 2 PRs if too large).

## Todo List
- [ ] MassTransit version locked (Q1 resolved)
- [ ] Caching project + tests
- [ ] Messaging project + tests
- [ ] Observability project + tests
- [ ] Persistence project + tests (in-memory EF + outbox event flow)
- [ ] Web project + tests (middleware via TestServer)
- [ ] Auth project + tests (JWT validation with sample RSA key)
- [ ] All 6 added to sln
- [ ] Architecture: SharedKernel + Abstractions still reference NOTHING from these (verify NetArchTest in phase 09)

## Success Criteria
- `dotnet test` green across all BuildingBlocks test projects.
- Each `Add{X}()` extension callable from a fresh minimal-API ASP.NET Core test host.
- Outbox event flow: domain event raised → SaveChanges → row in `outbox_messages` table (verified in unit test using EF in-memory or sqlite).

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| EF Core 9.x doesn't fully support net10.0 yet | M | H | Pre-flight via sandbox; fallback to MultiTargeting `<TargetFrameworks>net10.0;net9.0</TargetFrameworks>` for Persistence only |
| MassTransit 8.x deprecates fast / security CVE | M | M | Wrap behind `IEventBus` so swap is one impl change |
| Outbox transaction semantics broken if consumer doesn't use `IUnitOfWork` | H | H | Make `SaveChangesAsync` private-impl + only `IUnitOfWork.SaveChangesAsync` exposed; document loudly |
| Observability OTLP endpoint not present in dev breaks startup | M | L | Make OTel exporter conditional on `OTEL_EXPORTER_OTLP_ENDPOINT` env var |
| 6 projects in one PR is unreviewable | H | M | Split into two PRs: (5a) Caching+Messaging+Observability, (5b) Persistence+Web+Auth |

## Security Considerations
- JWT validation: enforce `ValidateIssuerSigningKey`, `ValidateAudience`, `ValidateIssuer`, `RequireSignedTokens`, `ClockSkew = TimeSpan.FromMinutes(1)`.
- ProblemDetails: never leak stack traces in non-Development environments. Gated by `IHostEnvironment.IsDevelopment()`.
- Outbox messages contain serialized domain payloads — treat as PII-sensitive at rest.

## Next Steps
Unblocks phase 06 (compose) — but compose can run independently; sequencing is to validate impls before building the runtime stack they connect to.
Unblocks phase 08 (template references all of these).
