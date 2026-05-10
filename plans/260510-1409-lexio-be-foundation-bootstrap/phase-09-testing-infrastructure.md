# Phase 09 — Testing infrastructure

## Context Links
- Doc §5.5 coverage thresholds (≥70% unit, ≥50% integration)
- Doc §6.7.7 NetArchTest examples
- Phase 03 SharedKernel tests (precedent)
- Phase 08 template test projects (consumers)

## Overview
- Priority: P1
- Status: pending
- Brief: Shared `Lexio.TestUtils` library + `.runsettings` + Testcontainers fixtures + global architecture tests.

## Key Insights
- xUnit v3 (already pinned phase 02) — uses different runner; `xunit.runner.visualstudio` v3.x compatible.
- Testcontainers needs Docker available; CI will use docker-in-docker on GH Actions Linux runners.
- Coverage via Coverlet + ReportGenerator (added as dotnet tool).
- Architecture tests live as a top-level test project that scans every assembly (not just per-service).

## Requirements
- Functional:
  - `tests/_shared/Lexio.TestUtils/` — common fixtures (PostgresFixture, MongoFixture, RedisFixture, RabbitMqFixture using Testcontainers); builders; assertion helpers.
  - `tests/architecture/Lexio.Architecture.Tests/` — repo-wide NetArchTest covering all Lexio.* assemblies.
  - `.runsettings` at root with coverage config + thresholds.
  - `scripts/coverage.sh` aggregates coverage and generates HTML.
- NFR: integration test fixture startup ≤10s per container.

## Architecture
```
tests/
├── _shared/
│   └── Lexio.TestUtils/
│       ├── Lexio.TestUtils.csproj
│       ├── Fixtures/
│       │   ├── PostgresFixture.cs
│       │   ├── MongoFixture.cs
│       │   ├── RedisFixture.cs
│       │   └── RabbitMqFixture.cs
│       ├── Builders/.gitkeep
│       └── TestClock.cs               (IClock test impl)
├── architecture/
│   └── Lexio.Architecture.Tests/
│       ├── Lexio.Architecture.Tests.csproj
│       ├── DomainLayerTests.cs
│       ├── ApplicationLayerTests.cs
│       ├── InfrastructureLayerTests.cs
│       └── BuildingBlocksTests.cs
├── shared/
│   ├── Lexio.SharedKernel.Tests/         (phase 03)
│   └── Lexio.BuildingBlocks.*.Tests/     (phase 04/05)
└── ...
```

## Related Code Files
Create:
- `tests/_shared/Lexio.TestUtils/...`
- `tests/architecture/Lexio.Architecture.Tests/...`
- `.runsettings`
- `scripts/coverage.sh`

## Implementation Steps
1. Branch `feat/be-test-infra` off `feat/be-service-template`.
2. Create `Lexio.TestUtils` classlib. Add refs: `Testcontainers`, `Testcontainers.PostgreSql`, `Testcontainers.MongoDb`, `Testcontainers.Redis`, `Testcontainers.RabbitMq`, `xunit.v3`.
3. Implement fixtures using xUnit v3 `IAsyncLifetime`:
   ```csharp
   public sealed class PostgresFixture : IAsyncLifetime {
     public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
       .WithImage("postgres:17-alpine").Build();
     public string ConnectionString => Container.GetConnectionString();
     public ValueTask InitializeAsync() => Container.StartAsync();
     public ValueTask DisposeAsync() => Container.DisposeAsync();
   }
   ```
   Plus Mongo/Redis/RabbitMq variants.
4. Implement `TestClock : IClock` (mutable `UtcNow`).
5. Architecture tests project — references EVERY Lexio.* assembly via project ref OR via assembly load. Test cases:
   - `Lexio.SharedKernel` ShouldNot HaveDependencyOn `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, `MassTransit`, `MediatR`, `Mediator`.
   - `Lexio.BuildingBlocks.Abstractions` ShouldNot HaveDependencyOn `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`.
   - For each service: `Lexio.{S}.Domain` ShouldNot HaveDependencyOn `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`, `Lexio.{S}.Infrastructure`, `Lexio.{S}.Application`, `Lexio.{S}.Api`.
   - `Lexio.{S}.Application` ShouldNot HaveDependencyOn `Lexio.{S}.Infrastructure`, `Lexio.{S}.Api`, `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`.
   - Handlers ending with `Handler`. Validators ending with `Validator`.
   - Note: at this phase no concrete service exists. Architecture tests scan via assembly-name pattern at runtime; harmless (no assemblies match → trivially pass) until template-generated services land.
6. `.runsettings`:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <RunSettings>
     <DataCollectionRunSettings>
       <DataCollectors>
         <DataCollector friendlyName="XPlat code coverage">
           <Configuration>
             <Format>cobertura</Format>
             <Exclude>[*.Tests]*,[Lexio.TestUtils]*,[*]*Migrations*</Exclude>
             <ExcludeByAttribute>GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
           </Configuration>
         </DataCollector>
       </DataCollectors>
     </DataCollectionRunSettings>
   </RunSettings>
   ```
7. `scripts/coverage.sh`:
   ```bash
   #!/usr/bin/env bash
   set -euo pipefail
   rm -rf TestResults coverage-report
   dotnet test --collect:"XPlat Code Coverage" --settings .runsettings --results-directory TestResults
   dotnet tool restore
   dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:"Html;TextSummary"
   cat coverage-report/Summary.txt
   ```
   Add `dotnet-reportgenerator-globaltool` to `.config/dotnet-tools.json`.
8. Document threshold enforcement in CI (phase 11): parse Cobertura, fail < 70% line coverage on production code.
9. Commit: `feat(be-test): add TestUtils, architecture tests, coverage settings`.

## Todo List
- [ ] `Lexio.TestUtils` with 4 Testcontainer fixtures + TestClock
- [ ] `Lexio.Architecture.Tests` with layer + naming rules
- [ ] `.runsettings` with coverage config
- [ ] `scripts/coverage.sh` end-to-end works locally
- [ ] All test projects build + run

## Success Criteria
- `dotnet test` green on all phase 03–08 tests.
- `bash scripts/coverage.sh` produces HTML + text summary.
- Architecture tests fail intentionally if a sample violation introduced (mutation test).

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Testcontainers slow / flaky on CI | M | M | Reuse containers via `[Collection]` xUnit fixtures; pin image versions matching compose |
| NetArchTest scans wrong assemblies (loaded vs referenced) | M | H | Force-load via `Assembly.Load("Lexio.X")`; gate via dummy reference in csproj |
| Coverage false-low due to missing test projects | L | M | Exclude `*.Tests`, `*.Architecture.Tests`, Migrations |
| xUnit v3 ecosystem still maturing | M | M | Pin to latest stable; revisit if FluentAssertions v7 incompatible |

## Security Considerations
- Test containers run with default creds — never reuse on shared CI without isolation; GH Actions runner is ephemeral so safe.
- Avoid logging real secrets in test output.

## Next Steps
Unblocks phase 10 (hooks reference test scripts) and phase 11 (CI runs coverage script).
