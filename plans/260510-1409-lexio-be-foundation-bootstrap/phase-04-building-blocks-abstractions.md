# Phase 04 — Lexio.BuildingBlocks.Abstractions (interfaces only)

## Context Links
- Doc §6.7.6 — outer layers depend on inner; abstractions live inward
- Phase 03 SharedKernel
- Researcher A — IClock, ICurrentUserAccessor patterns

## Overview
- Priority: P1
- Status: pending
- Brief: Cross-cutting interfaces (event bus, outbox, distributed cache, clock, current user, logger). Implementations land phase 05.

## Key Insights
- Application layers of all 7 services depend on this — must be stable before services scale.
- Keep interfaces small (ISP) — many tiny interfaces > one mega-interface.
- No `Microsoft.AspNetCore.*` here. Only `Microsoft.Extensions.*` allowed if necessary.

## Requirements
- Functional interfaces:
  - `IEventBus` — publish + consume integration events
  - `IIntegrationEvent` marker
  - `IOutbox` — append + dispatch (used by Infrastructure layer)
  - `IDistributedCache` — typed wrapper over byte cache (or just re-export `Microsoft.Extensions.Caching.Distributed.IDistributedCache` extension methods)
  - `IClock` (re-import from SharedKernel — actually keep IClock in SharedKernel; here add nothing)
  - `ICurrentUserAccessor` — `Guid? UserId`, `string? Email`, `IReadOnlyList<string> Roles`
  - `ITenantAccessor` (placeholder for multi-tenancy if ever needed — defer; YAGNI; SKIP)
  - `IUnitOfWork` — `Task<int> SaveChangesAsync(CancellationToken)`
  - `IDateTimeProvider` (synonym for IClock; pick one; KEEP IClock only)

## Architecture
```
src/shared/Lexio.BuildingBlocks.Abstractions/
├── Lexio.BuildingBlocks.Abstractions.csproj
├── Messaging/
│   ├── IEventBus.cs
│   ├── IIntegrationEvent.cs
│   └── IIntegrationEventHandler.cs
├── Outbox/
│   ├── IOutbox.cs
│   └── OutboxMessage.cs           (POCO record)
├── Caching/
│   └── ILexioCache.cs              (typed get/set/remove)
├── Auth/
│   └── ICurrentUserAccessor.cs
└── Persistence/
    └── IUnitOfWork.cs
```

Refs: SharedKernel only. Plus optional `Microsoft.Extensions.Caching.Abstractions` for `IDistributedCache` adapter signature, `Microsoft.Extensions.Logging.Abstractions`.

## Related Code Files
Create as listed above + matching test project `tests/shared/Lexio.BuildingBlocks.Abstractions.Tests/` (smoke contract tests; mostly empty).

## Implementation Steps
1. Branch `feat/be-bb-abstractions` off `feat/be-shared-kernel`.
2. `dotnet new classlib -n Lexio.BuildingBlocks.Abstractions -o src/shared/Lexio.BuildingBlocks.Abstractions --framework net10.0`.
3. Add ProjectReference to `Lexio.SharedKernel`. Add PackageReferences:
   - `Microsoft.Extensions.Caching.Abstractions`
   - `Microsoft.Extensions.Logging.Abstractions`
   (Pin in `Directory.Packages.props` if not yet present.)
4. Implement interfaces above. Each file ≤80 lines.
5. `OutboxMessage` POCO: `record OutboxMessage(Guid Id, string Type, string Payload, DateTimeOffset OccurredAt, DateTimeOffset? ProcessedAt);`
6. Add to sln. `dotnet build` clean.
7. Smoke tests: assert `IEventBus` is an interface, `IIntegrationEvent` marker shape.
8. Commit: `feat(be-shared): add BuildingBlocks.Abstractions interfaces`.

## Todo List
- [ ] All 8 interface files created
- [ ] Refs only SharedKernel + Microsoft.Extensions.* abstractions
- [ ] Build clean
- [ ] Smoke tests pass

## Success Criteria
- `dotnet list ... package` shows zero `AspNetCore` deps.
- Architecture test (added later in phase 09) passes: `Lexio.BuildingBlocks.Abstractions ShouldNot HaveDependencyOn "Microsoft.AspNetCore"`.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Interface churn ripples to all services | M | H | Lock signatures via xUnit reflection-based contract tests in phase 09 |
| `IEventBus` design too thin/fat for actual messaging needs | M | M | Validate against MassTransit + Confluent.Kafka APIs in phase 05 BEFORE finalising |

## Security Considerations
- `ICurrentUserAccessor` exposes user identity; ensure no PII (email-only ok for v0).

## Next Steps
Unblocks phase 05 (impls) and phase 06 (compose stack uses these contracts later).
