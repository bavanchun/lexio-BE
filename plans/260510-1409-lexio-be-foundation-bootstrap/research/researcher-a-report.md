# Researcher A — .NET 10 Clean Architecture, DDD building blocks, Mediator licensing

## Mediator licensing landscape (CRITICAL)

- **MediatR** went commercial 2025-07 with v13 (Lucky Penny Software, RPL-1.5 + commercial dual). v12.x and earlier remain MIT but archived.
- **MassTransit** also moved to commercial license (v9+); v8.x remains MIT but no longer feature-developed.
- **AutoMapper** same fate. **Mapster** (doc choice) is still MIT.
- **Recommendation for Lexio:** use `Mediator` by martinothamar/Mediator (MIT, source-generator, AOT-friendly, faster than MediatR). API near-identical: `IRequest<T>`, `IRequestHandler<T,R>`, `IPipelineBehavior<T,R>`. Doc spec says "MediatR" but locked decision overrides — swap to OSS `Mediator`.
- **MassTransit replacement options:**
  - (a) Pin MassTransit 8.x (last MIT) — known feature surface, but stale.
  - (b) Use raw `RabbitMQ.Client` + `Confluent.Kafka` wrapped in our own `IEventBus` abstraction.
  - (c) `Wolverine` (Jasper successor, MIT) — modern, transactional outbox built-in.
- **Decision:** wrap behind `IEventBus`/`IOutbox` in `BuildingBlocks.Abstractions` so swap is one impl. Pick (a) MassTransit 8.x for v0 since it covers RabbitMQ + outbox + sagas; revisit before prod. Flag as unresolved.

## SharedKernel layout (DDD primitives, no framework deps)

- `Entity<TId>` — abstract, Id property, equality by id.
- `AggregateRoot<TId> : Entity<TId>` — `IReadOnlyCollection<IDomainEvent> DomainEvents` + `Raise(IDomainEvent)` + `ClearDomainEvents()`.
- `ValueObject` — abstract, `IEnumerable<object?> GetEqualityComponents()` + structural equality + `==`/`!=`.
- `IDomainEvent` — marker; consider `IDomainEvent { Guid EventId; DateTime OccurredOn; }` as default record interface.
- `Result` / `Result<T>` — railway-oriented. Static `Success`, `Failure(Error)`. `Error` record `{ string Code, string Message, ErrorType Type }`. ErrorType enum: `Validation, NotFound, Conflict, Unauthorized, Forbidden, Internal`.
- `Maybe<T>` — wraps optional. `HasValue`, `Value`, implicit conversions.
- **Strong-typed IDs:** prefer `readonly record struct UserId(Guid Value)` with EF Core value converter helper. `Vogen` library is the slick path but adds source-gen dependency. Defer Vogen — provide one hand-written example pattern in SharedKernel docs.
- **No `OneOf`** — keep one Result type. C# 14 / .NET 10 doesn't ship discriminated unions yet (still in preview discussion); custom Result is fine.

## .NET 10 specifics worth knowing

- `TargetFramework=net10.0` — supported by SDK 10.0.203.
- `LangVersion=latest` gives C# 14 (params collections, field keyword stable, etc.).
- **Central package management:** `Directory.Packages.props` with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`. Csproj uses `<PackageReference Include="X" />` with NO Version. Pitfall: transitive pinning needs `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>`.
- **Roslyn analyzers + SonarAnalyzer.CSharp** added once in `Directory.Build.props` `<ItemGroup>` propagates to all projects. Use `PrivateAssets="all"` so they don't leak to consumers.
- **Nullable enable + TreatWarningsAsErrors** — apply globally in Build.props; allow per-project opt-out for generated code via `<NoWarn>` on Migrations folder.
- Primary constructors useful for handlers/repositories — keep DI minimal boilerplate.

## Pitfalls

- Don't reference `Microsoft.AspNetCore.*` from SharedKernel/BuildingBlocks.Abstractions — keep abstractions pure.
- BuildingBlocks should ship as project references (monorepo) NOT NuGet for v0; cleaner refactor paths.
- Each Infra project gets its own EF migrations assembly — don't share DbContexts across services.
- `IClock` (return `DateTimeOffset.UtcNow`) is essential for testability — inject everywhere instead of static `DateTime.UtcNow`.

## Sources
- [MediatR & MassTransit going commercial — Milan Jovanović](https://www.milanjovanovic.tech/blog/mediatr-and-masstransit-going-commercial-what-this-means-for-you)
- [Mediator (martinothamar) GitHub](https://github.com/martinothamar/Mediator)
- [Cortex.Mediator OSS alternative](https://medium.com/@eneshoxha_65350/cortex-mediator-a-free-open-source-alternative-to-mediatr-for-cqrs-in-net-59534e1305c7)
- [Jimmy Bogard — AutoMapper & MediatR licensing update](https://www.jimmybogard.com/automapper-and-mediatr-licensing-update/)

## Unresolved
- MassTransit 8.x pin vs Wolverine vs raw clients — pick before phase 05.
- Strong-typed IDs: hand-rolled vs Vogen source-gen.
