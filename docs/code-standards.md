# Code Standards

> **Canonical source:** `docs/Lexio_Complete_Documentation.docx` §6.7.
> This file summarises BE-specific rules only.

## General

- Language: C# 13 (`LangVersion=latest` in `Directory.Build.props`)
- Nullable: enabled everywhere (`Nullable=enable`)
- Warnings: all warnings are errors (`TreatWarningsAsErrors=true`)
- Analyzers: SonarAnalyzer.CSharp + Microsoft.CodeAnalysis.NetAnalyzers (AnalysisMode=Recommended)

## Naming

| Construct | Convention | Example |
|-----------|-----------|---------|
| Class/Record/Interface | PascalCase | `UserCreatedEvent` |
| Method | PascalCase | `HandleAsync` |
| Private field | `_camelCase` | `_clock` |
| Local variable | camelCase | `domainEvents` |
| Constant | PascalCase | `MaxRetryCount` |

## File Organisation

- One type per file; filename matches type name.
- Files > 200 lines: split into focused modules.
- Use `partial` only for source-generated types (`Program`, EF snapshots).

## Error Handling

- Use `Result<T>` / `Error` for domain operations — no exceptions for expected failures.
- Use exceptions only for programming errors (null guard, invalid operation).
- All async methods must have explicit `CancellationToken` parameter.

## Architecture Rules (enforced by NetArchTest)

- `Domain` → no EF Core, no AspNetCore, no MassTransit, no Mediator.
- `Application` → no Infrastructure, no AspNetCore, no EF Core.
- `Infrastructure` → no Api.
- `Abstractions` → no AspNetCore, no EF Core.

## Testing

- Naming: `MethodName_StateUnderTest_ExpectedBehavior`
- xUnit v3 + FluentAssertions 7 + Moq 4
- Testcontainers for integration tests; `TestClock` for time-dependent unit tests.
- Coverage thresholds: ≥70% line (production code); enforced in CI.

## Commit Convention

Conventional Commits enforced by Husky commit-msg hook. See [ADR 0001](architecture/0001-monorepo-with-dotnet-10.md).
