# Phase 03 — Lexio.SharedKernel

## Context Links
- Doc §6.7.6 (Domain references nothing); §6.7.3 shows `shared/Lexio.SharedKernel/`
- Researcher A — DDD primitives, Result pattern

## Overview
- Priority: P1
- Status: pending
- Brief: Pure-C# DDD primitives. Zero framework references. Reused by every service Domain layer.

## Key Insights
- Must compile with zero `Microsoft.*` (apart from BCL via implicit usings) and zero NuGet PackageReferences other than analyzers.
- `Result<T>` is the project's universal error transport — every Application handler returns one.
- `IDomainEvent` shape is locked in this phase; later changes are breaking for all services.

## Requirements
- Functional:
  - `Entity<TId>` with `Id` + equality
  - `AggregateRoot<TId>` with domain event collection
  - `ValueObject` with structural equality
  - `IDomainEvent` interface
  - `Result`, `Result<T>`, `Error`, `ErrorType`
  - `Maybe<T>` optional wrapper
  - `IClock` interface (impl lives in BuildingBlocks phase 05)
- NFR: 100% xUnit coverage on these primitives (they will be exercised forever).

## Architecture
```
src/shared/Lexio.SharedKernel/
├── Lexio.SharedKernel.csproj    (no PackageReferences, only inherited analyzers)
├── Domain/
│   ├── Entity.cs
│   ├── AggregateRoot.cs
│   └── ValueObject.cs
├── Events/
│   └── IDomainEvent.cs
├── Primitives/
│   ├── Result.cs
│   ├── ResultOfT.cs
│   ├── Error.cs
│   ├── ErrorType.cs
│   └── Maybe.cs
└── Time/
    └── IClock.cs

tests/shared/Lexio.SharedKernel.Tests/
├── Lexio.SharedKernel.Tests.csproj
├── EntityTests.cs
├── ValueObjectTests.cs
├── ResultTests.cs
└── MaybeTests.cs
```

Project refs: tests project → SharedKernel. SharedKernel → none.

## Related Code Files
Create all files listed above.

## Implementation Steps
1. Branch `feat/be-shared-kernel` off `feat/be-build-props`.
2. `mkdir -p src/shared/Lexio.SharedKernel tests/shared/Lexio.SharedKernel.Tests`.
3. `dotnet new classlib -n Lexio.SharedKernel -o src/shared/Lexio.SharedKernel --framework net10.0`. Delete `Class1.cs`.
4. Implement files (each ≤200 lines, see snippet guidance below). Key shapes:
   - `Entity<TId>`: `public TId Id { get; protected set; }` + override `Equals` + `GetHashCode` + `==`/`!=`.
   - `AggregateRoot<TId>`: `private readonly List<IDomainEvent> _domainEvents = new(); public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly(); protected void Raise(IDomainEvent @event); public void ClearDomainEvents();`.
   - `ValueObject`: `protected abstract IEnumerable<object?> GetEqualityComponents();` then `Equals` compares sequences.
   - `Result`: `IsSuccess`, `IsFailure`, `Error`, static `Success()`, `Failure(Error)`. `Result<T>`: adds `Value` (throws if failure).
   - `Error`: `record Error(string Code, string Message, ErrorType Type)` + static helpers `Validation`, `NotFound`, `Conflict`, etc.
   - `Maybe<T>`: `HasValue`, `Value`, `From(T?)`, implicit conversion from `T`.
   - `IClock`: `DateTimeOffset UtcNow { get; }`.
5. `dotnet sln add src/shared/Lexio.SharedKernel/Lexio.SharedKernel.csproj`.
6. `dotnet new xunit -n Lexio.SharedKernel.Tests -o tests/shared/Lexio.SharedKernel.Tests --framework net10.0` then convert to xUnit v3 (replace `xunit` package refs with `xunit.v3` in csproj).
7. Add `FluentAssertions` reference to test project. Add ProjectReference to `Lexio.SharedKernel`.
8. Write tests: equality on Entity, structural equality on ValueObject, Result success/failure transitions, Maybe.From(null) yields no value.
9. `dotnet build` — must be 0 warnings. `dotnet test` — green.
10. Commit: `feat(be-shared): add SharedKernel DDD primitives + tests`.

## Todo List
- [ ] `Lexio.SharedKernel` csproj has zero non-analyzer PackageReferences
- [ ] All 9 files implemented per skeleton
- [ ] Tests cover Entity, ValueObject, Result, Maybe
- [ ] `dotnet build` clean, `dotnet test` green
- [ ] Solution updated

## Success Criteria
- `dotnet list src/shared/Lexio.SharedKernel package` shows only analyzer entries.
- Coverage ≥90% for Lexio.SharedKernel namespace (run `dotnet test --collect:"XPlat Code Coverage"`).

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| ValueObject equality bug ripples through services | L | H | Lock with property-style xUnit theory tests covering nested VOs |
| Result<T>.Value access on failure throws unclearly | L | M | Throw `InvalidOperationException` with explicit message; test it |
| Future need for async Result chaining | M | L | Leave room: don't seal Result; later add `Bind`/`Map` ext methods in same namespace |

## Security Considerations
- N/A (pure types).

## Next Steps
Unblocks phase 04 (Abstractions reference SharedKernel).
