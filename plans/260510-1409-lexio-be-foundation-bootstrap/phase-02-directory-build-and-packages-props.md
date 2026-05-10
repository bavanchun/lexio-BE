# Phase 02 — Directory.Build.props + Directory.Packages.props + Lexio.sln

## Context Links
- Doc §6.7.6 (dependency rules), §9.1 (tech stack)
- Researcher A — central package management gotchas
- Phase 01 (.editorconfig + gitignore must be present)

## Overview
- Priority: P1
- Status: pending
- Brief: Establish global MSBuild defaults, central package version management, analyzers, and the empty `Lexio.sln`.

## Key Insights
- `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` requires SDK ≥ 7. Works on 10.
- `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` makes transitive deps honour `Directory.Packages.props`.
- Roslyn analyzers + SonarAnalyzer.CSharp added in `Directory.Build.props` propagate to every project — `PrivateAssets="all"` prevents leaking to consumers.
- `TreatWarningsAsErrors=true` will fail the build on any analyzer warning — accept that pain now to keep code clean.

## Requirements
- Functional: every csproj created in later phases auto-inherits net10.0 + nullable + analyzers.
- NFR: build reproducibility (`<Deterministic>true</Deterministic>`, `<ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)'=='true'">true</ContinuousIntegrationBuild>`).

## Architecture
Repo root MSBuild files form a tree-walked inheritance chain. Every csproj at any depth inherits.

## Related Code Files
Create:
- `Directory.Build.props` (root)
- `Directory.Packages.props` (root)
- `Directory.Build.targets` (root, near-empty placeholder for later)
- `Lexio.sln`
- `global.json` pinning SDK
- `nuget.config` (optional, for pinning sources to nuget.org only)
- `.config/dotnet-tools.json` (manifest for husky, dotnet-outdated, dotnet-ef later)

## Implementation Steps
1. Branch from phase 01: `git checkout -b feat/be-build-props feat/be-foundation-init`.
2. `cat > global.json` with `{ "sdk": { "version": "10.0.203", "rollForward": "latestFeature" } }`.
3. `Directory.Build.props`:
   ```xml
   <Project>
     <PropertyGroup>
       <TargetFramework>net10.0</TargetFramework>
       <LangVersion>latest</LangVersion>
       <Nullable>enable</Nullable>
       <ImplicitUsings>enable</ImplicitUsings>
       <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
       <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
       <AnalysisLevel>latest</AnalysisLevel>
       <AnalysisMode>Recommended</AnalysisMode>
       <Deterministic>true</Deterministic>
       <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)'=='true'">true</ContinuousIntegrationBuild>
       <Company>Lexio</Company>
       <Authors>Lexio</Authors>
       <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- missing XML doc -->
     </PropertyGroup>
     <ItemGroup>
       <PackageReference Include="SonarAnalyzer.CSharp" PrivateAssets="all" />
       <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" PrivateAssets="all" />
     </ItemGroup>
   </Project>
   ```
4. `Directory.Packages.props` — central pinning. Initial set:
   ```xml
   <Project>
     <PropertyGroup>
       <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
       <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
     </PropertyGroup>
     <ItemGroup>
       <!-- Analyzers -->
       <PackageVersion Include="SonarAnalyzer.CSharp" Version="9.32.*" />
       <PackageVersion Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" />
       <!-- DDD / Mediator -->
       <PackageVersion Include="Mediator.SourceGenerator" Version="2.*" />
       <PackageVersion Include="Mediator.Abstractions" Version="2.*" />
       <PackageVersion Include="FluentValidation" Version="11.*" />
       <PackageVersion Include="Mapster" Version="7.*" />
       <!-- ASP.NET / EF -->
       <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.*" />
       <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.*" />
       <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.*" />
       <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.*" />
       <PackageVersion Include="MongoDB.Driver" Version="3.*" />
       <PackageVersion Include="StackExchange.Redis" Version="2.*" />
       <PackageVersion Include="Elastic.Clients.Elasticsearch" Version="8.*" />
       <!-- Auth -->
       <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.*" />
       <PackageVersion Include="OpenIddict.AspNetCore" Version="6.*" />
       <PackageVersion Include="OpenIddict.EntityFrameworkCore" Version="6.*" />
       <!-- Messaging -->
       <PackageVersion Include="MassTransit" Version="8.3.*" />
       <PackageVersion Include="MassTransit.RabbitMQ" Version="8.3.*" />
       <PackageVersion Include="Confluent.Kafka" Version="2.*" />
       <!-- Observability -->
       <PackageVersion Include="Serilog" Version="4.*" />
       <PackageVersion Include="Serilog.AspNetCore" Version="9.*" />
       <PackageVersion Include="Serilog.Sinks.OpenTelemetry" Version="4.*" />
       <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
       <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
       <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
       <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
       <!-- Testing -->
       <PackageVersion Include="xunit.v3" Version="1.*" />
       <PackageVersion Include="xunit.runner.visualstudio" Version="3.*" />
       <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.*" />
       <PackageVersion Include="Moq" Version="4.*" />
       <PackageVersion Include="FluentAssertions" Version="7.*" />
       <PackageVersion Include="NetArchTest.Rules" Version="1.*" />
       <PackageVersion Include="Testcontainers" Version="4.*" />
       <PackageVersion Include="Testcontainers.PostgreSql" Version="4.*" />
       <PackageVersion Include="Testcontainers.MongoDb" Version="4.*" />
       <PackageVersion Include="Testcontainers.Redis" Version="4.*" />
       <PackageVersion Include="Testcontainers.RabbitMq" Version="4.*" />
       <PackageVersion Include="coverlet.collector" Version="6.*" />
     </ItemGroup>
   </Project>
   ```
   NOTE: pin exact versions before merging — `*` shown for plan readability. Run `dotnet list package --outdated` after restore to lock.
5. `Directory.Build.targets` — empty `<Project></Project>` placeholder for later.
6. `nuget.config` restricting sources to nuget.org only:
   ```xml
   <configuration>
     <packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources>
   </configuration>
   ```
7. `dotnet new sln -n Lexio` at repo root.
8. `dotnet new tool-manifest` → `.config/dotnet-tools.json`. Install: `dotnet tool install Husky` (for phase 10), `dotnet tool install dotnet-outdated-tool`, `dotnet tool install dotnet-ef`.
9. Smoke check: `dotnet restore` (succeeds with no projects). `dotnet build` no-ops cleanly.
10. Commit: `feat(be-build): add Directory.Build.props, central package management, sln`.

## Todo List
- [ ] `global.json` pins SDK 10.0.203
- [ ] `Directory.Build.props` with net10.0 + nullable + analyzers
- [ ] `Directory.Packages.props` with CPM + locked versions
- [ ] `nuget.config` source restriction
- [ ] `Lexio.sln` created
- [ ] `.config/dotnet-tools.json` with husky, dotnet-outdated, dotnet-ef
- [ ] `dotnet restore` succeeds

## Success Criteria
- `dotnet --info` reports SDK 10.0.203 selected.
- `dotnet restore` exit 0 with no projects (no-op clean).
- A throwaway `dotnet new classlib -o /tmp/probe` linked into `Lexio.sln` builds cleanly with analyzers active (then revert).

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Some pinned packages don't yet support net10.0 TFM | M | H | Pre-flight `dotnet add` test on a sandbox classlib; fallback to nearest TFM via `<TargetFrameworks>net10.0;net9.0</TargetFrameworks>` only if absolutely needed |
| `TreatWarningsAsErrors=true` blocks unrelated PRs later | M | M | Allow per-project opt-out (`<TreatWarningsAsErrors>false</TreatWarningsAsErrors>`) for generated code only (e.g. EF Migrations folder) |
| Sonar + NetAnalyzers double-flag same rule | L | L | Both use distinct rule prefixes (S vs CA); accept slight overlap |

## Security Considerations
- `nuget.config` pinning to nuget.org-only prevents accidental package source confusion attacks.
- Add `<NuGetAuditMode>direct</NuGetAuditMode>` (NuGet 6.10+) to fail builds on known CVEs in direct deps.

## Next Steps
Unblocks phase 03 (SharedKernel) — needs CPM + analyzers in place.
