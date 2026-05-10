# Phase 08 — Custom `dotnet new lexio-service` template

## Context Links
- Doc §6.7.3 (folder structure example)
- Doc §12.10 (naming `Lexio.{Service}.{Layer}`)
- Researcher C — template.json schema, packing
- Phase 05 BuildingBlocks (template references them)

## Overview
- Priority: P1
- Status: pending
- Brief: One command spits out a full Clean Architecture service skeleton. Future business logic = fill in stubs.

## Key Insights
- Sentinel name `Service1` replaced by `-n` flag value (template `sourceName`).
- Template package itself is a separate csproj (`PackageType=Template`); template content lives under `content/`.
- Architecture tests pre-wired in template via NetArchTest references.
- Smoke test: CI must run `dotnet new lexio-service -n Smoke -o /tmp/smoke && dotnet build /tmp/smoke` (added in phase 11).

## Requirements
- Functional: `dotnet new install ./templates/Lexio.ServiceTemplate` then `dotnet new lexio-service -n Identity -o src/services/Identity` produces 4 src + 4 test projects, all building, with dummy controller + smoke test.
- NFR: generated service compiles in <30s on warm cache.

## Architecture
```
templates/Lexio.ServiceTemplate/
├── Lexio.ServiceTemplate.csproj
├── README.md
├── .template.config/
│   └── template.json
└── content/
    ├── Lexio.Service1.Domain/
    │   ├── Lexio.Service1.Domain.csproj           (ref: Lexio.SharedKernel)
    │   ├── Entities/.gitkeep
    │   ├── ValueObjects/.gitkeep
    │   ├── Events/.gitkeep
    │   ├── Exceptions/.gitkeep
    │   ├── Interfaces/.gitkeep
    │   └── Services/.gitkeep
    ├── Lexio.Service1.Application/
    │   ├── Lexio.Service1.Application.csproj       (refs: Domain, Mediator.Abstractions, FluentValidation, Mapster, BB.Abstractions)
    │   ├── DependencyInjection.cs
    │   ├── Common/Behaviors/ValidationBehavior.cs  (Mediator pipeline)
    │   ├── Common/Behaviors/LoggingBehavior.cs
    │   ├── Common/Mappings/.gitkeep
    │   ├── Features/.gitkeep
    │   ├── Contracts/.gitkeep
    │   └── Interfaces/.gitkeep
    ├── Lexio.Service1.Infrastructure/
    │   ├── Lexio.Service1.Infrastructure.csproj    (refs: Application, BB.Persistence, BB.Caching, BB.Messaging, BB.Auth)
    │   ├── DependencyInjection.cs
    │   ├── Persistence/Service1DbContext.cs        (inherits LexioDbContextBase)
    │   └── Persistence/Migrations/.gitkeep
    ├── Lexio.Service1.Api/
    │   ├── Lexio.Service1.Api.csproj               (refs: Application, Infrastructure, BB.Web, BB.Observability)
    │   ├── Program.cs                               (composition root)
    │   ├── Dockerfile
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── Controllers/HealthController.cs          (smoke endpoint)
    └── tests/
        ├── Lexio.Service1.Domain.Tests/
        ├── Lexio.Service1.Application.Tests/
        ├── Lexio.Service1.Infrastructure.Tests/   (Testcontainers later)
        └── Lexio.Service1.Api.Tests/
            └── ArchitectureTests.cs                 (NetArchTest enforcing layer rules)
```

## Related Code Files
All files under `templates/Lexio.ServiceTemplate/`. Plus `scripts/new-service.sh` wrapper.

## Implementation Steps
1. Branch `feat/be-service-template` off `feat/be-bb-impls`.
2. Create folder tree above. Each csproj uses `<TargetFramework>net10.0</TargetFramework>` (inherited from Directory.Build.props in consumer repo, but ALSO declare locally so template builds in isolation if needed).
3. Write `template.json`:
   ```json
   {
     "$schema": "http://json.schemastore.org/template",
     "author": "Lexio",
     "classifications": ["Web", "Microservice", "Clean Architecture"],
     "name": "Lexio Microservice (Clean Architecture)",
     "identity": "Lexio.ServiceTemplate",
     "groupIdentity": "Lexio.ServiceTemplate",
     "shortName": "lexio-service",
     "tags": { "language": "C#", "type": "project" },
     "sourceName": "Service1",
     "preferNameDirectory": false,
     "primaryOutputs": [
       { "path": "Lexio.Service1.Api/Lexio.Service1.Api.csproj" }
     ]
   }
   ```
4. Write `Lexio.ServiceTemplate.csproj`:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <PackageType>Template</PackageType>
       <PackageId>Lexio.ServiceTemplate</PackageId>
       <Version>0.1.0</Version>
       <Title>Lexio Microservice Template</Title>
       <IncludeContentInPack>true</IncludeContentInPack>
       <ContentTargetFolders>content</ContentTargetFolders>
       <NoDefaultExcludes>true</NoDefaultExcludes>
       <NoWarn>NU5128</NoWarn>
       <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
       <IsPackable>true</IsPackable>
     </PropertyGroup>
     <ItemGroup>
       <Content Include="content/**/*" Exclude="content/**/bin/**;content/**/obj/**;**/.template.config/**" />
       <Compile Remove="**\*" />
     </ItemGroup>
   </Project>
   ```
5. Implement minimal Program.cs (Service1.Api):
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   builder.Services.AddLexioObservability("Lexio.Service1");
   builder.Services.AddLexioWeb();
   builder.Services.AddLexioAuth(builder.Configuration);
   builder.Services.AddService1Application();
   builder.Services.AddService1Infrastructure(builder.Configuration);
   builder.Services.AddControllers();
   builder.Services.AddHealthChecks();
   var app = builder.Build();
   app.UseLexioWeb();
   app.UseAuthentication();
   app.UseAuthorization();
   app.MapControllers();
   app.MapHealthChecks("/health");
   app.Run();
   ```
6. `HealthController` with `GET /smoke` → `Ok("Service1 alive")`.
7. `Service1DbContext : LexioDbContextBase` placeholder.
8. `ArchitectureTests.cs` in `Lexio.Service1.Api.Tests`:
   ```csharp
   [Fact] public void Domain_ShouldNot_DependOn_Infrastructure() { /* NetArchTest */ }
   [Fact] public void Application_ShouldNot_DependOn_Infrastructure() { ... }
   [Fact] public void Infrastructure_ShouldNot_DependOn_Api() { ... }
   [Fact] public void Handlers_ShouldEndWith_Handler() { ... }
   ```
9. `Dockerfile` (multi-stage): `mcr.microsoft.com/dotnet/sdk:10.0` build → `mcr.microsoft.com/dotnet/aspnet:10.0` runtime.
10. `templates/README.md`: install + use + add to sln steps.
11. `scripts/new-service.sh`:
    ```bash
    #!/usr/bin/env bash
    set -euo pipefail
    NAME="${1:?usage: new-service.sh <Name>}"
    dotnet new lexio-service -n "$NAME" -o "src/services/$NAME"
    for proj in "src/services/$NAME"/Lexio."$NAME".*/*.csproj "src/services/$NAME"/tests/*/*.csproj; do
      dotnet sln Lexio.sln add "$proj"
    done
    ```
12. Smoke: `dotnet new install ./templates/Lexio.ServiceTemplate`, then `bash scripts/new-service.sh Smoke`, then `dotnet build src/services/Smoke`. Cleanup: `rm -rf src/services/Smoke && dotnet sln remove ...` — the smoke output should NOT be committed.
13. Commit: `feat(be-template): add Lexio.ServiceTemplate dotnet-new template`.

## Todo List
- [ ] `templates/Lexio.ServiceTemplate/` folder created
- [ ] `template.json` with `sourceName: Service1`
- [ ] Template csproj with `PackageType=Template`
- [ ] All 4 src + 4 test project skeletons in content
- [ ] Pre-wired NetArchTest architecture tests
- [ ] Program.cs uses BB.* extensions
- [ ] Dockerfile multi-stage
- [ ] `scripts/new-service.sh` wrapper
- [ ] Smoke generation + build green
- [ ] `templates/README.md`

## Success Criteria
- `dotnet new install ./templates/Lexio.ServiceTemplate` succeeds.
- `bash scripts/new-service.sh Smoke && dotnet build src/services/Smoke` exits 0.
- Generated `Smoke` service has 4 + 4 projects, NetArchTest tests green, smoke endpoint reachable on `dotnet run`.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Template content `bin`/`obj` accidentally packed | H | M | Explicit `<Content Exclude>` + content gitignore |
| Future BB API change breaks all generated services | H | H | Document: regen template per major BB version; existing services hand-migrate |
| `Service1` token clashes with normal C# | L | L | Sentinel chosen for low collision risk; add doc warning |
| `dotnet sln add` glob misses some projects on shells without `**` | M | L | Use `find` instead of glob in bash script |

## Security Considerations
- Generated Dockerfile must not embed secrets. `appsettings.Development.json` has no secrets (use ENV/user-secrets).
- Health endpoint must not leak version/build info beyond what's safe (no secrets).

## Next Steps
Unblocks phase 09 (test infra + TestUtils used by template tests) and phase 11 (CI runs `dotnet new` smoke).
