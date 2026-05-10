# Researcher C — Custom `dotnet new` template authoring 2026

## Layout

```
templates/Lexio.ServiceTemplate/
├── .template.config/
│   ├── template.json
│   └── icon.png  (optional)
├── content/
│   ├── Lexio.Service1.Domain/
│   │   ├── Lexio.Service1.Domain.csproj
│   │   └── Class1.cs
│   ├── Lexio.Service1.Application/
│   ├── Lexio.Service1.Infrastructure/
│   ├── Lexio.Service1.Api/
│   └── tests/
│       ├── Lexio.Service1.Domain.Tests/
│       └── ...
├── Lexio.ServiceTemplate.csproj   (template package)
└── README.md
```

The literal token `Service1` (or any chosen sentinel) inside source files + filenames is what gets replaced.

## template.json essentials

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
  "symbols": {
    "Service": {
      "type": "parameter",
      "datatype": "string",
      "description": "Service bounded-context name (e.g. Identity, Vocabulary).",
      "replaces": "Service1",
      "fileRename": "Service1",
      "isRequired": true
    },
    "Framework": {
      "type": "parameter",
      "datatype": "choice",
      "choices": [{ "choice": "net10.0" }],
      "defaultValue": "net10.0",
      "replaces": "net10.0"
    }
  },
  "primaryOutputs": [
    { "path": "Lexio.Service1.Api/Lexio.Service1.Api.csproj" }
  ]
}
```

- `sourceName` is shorthand: every occurrence of `Service1` in file content + paths replaced with the value passed to `-n`. We use the named symbol `Service` instead because we want a meaningful CLI flag and don't want the output **directory** to be auto-created (use `-o` for that).
- Actually: simplest approach is rely on `sourceName: "Service1"` + `dotnet new lexio-service -n Identity -o src/services/Identity`. The `-n` value becomes the replacement string. Drop the `Service` symbol unless we need it independent of `-n`.

## Packing & install

- Csproj for the template package:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <PackageType>Template</PackageType>
      <PackageId>Lexio.ServiceTemplate</PackageId>
      <Version>0.1.0</Version>
      <IncludeContentInPack>true</IncludeContentInPack>
      <ContentTargetFolders>content</ContentTargetFolders>
      <NoDefaultExcludes>true</NoDefaultExcludes>
      <NoWarn>NU5128</NoWarn>
      <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    </PropertyGroup>
    <ItemGroup>
      <Content Include="content/**/*" Exclude="content/**/bin/**;content/**/obj/**" />
      <Compile Remove="**\*" />
    </ItemGroup>
  </Project>
  ```
- Install from folder (dev): `dotnet new install ./templates/Lexio.ServiceTemplate`.
- Pack as nupkg (CI): `dotnet pack templates/Lexio.ServiceTemplate -o artifacts/`.
- Uninstall: `dotnet new uninstall Lexio.ServiceTemplate`.

## Pitfalls

- `bin/` and `obj/` from accidental builds in template content **WILL** ship — exclude in csproj + add `.gitignore` inside content folders.
- Token `Service1` must be unambiguous; avoid words that occur naturally (don't use `Demo`/`Test`).
- Don't put a top-level `.sln` inside template content — service should be added to root `.sln` post-generation via `dotnet sln add`.
- `template.json` must live at `<content-root>/.template.config/template.json` — easy to misplace.
- Conditional content (e.g. include gRPC service file only if `--grpc` flag) uses `//#if (Grpc)` C#-style preprocessor blocks + `<symbol>` of type `parameter` with `datatype:bool`.
- Unit-test the template: `dotnet new lexio-service -n Smoke -o /tmp/smoke && dotnet build /tmp/smoke` in CI.

## Wiring template into existing solution

After generation, add to root .sln:

```bash
dotnet new lexio-service -n Identity -o src/services/Identity
for proj in src/services/Identity/Lexio.Identity.*/*.csproj src/services/Identity/tests/**/*.csproj; do
  dotnet sln Lexio.sln add "$proj"
done
```

Provide as `scripts/new-service.sh <Name>` wrapper.

## Sources
- [template.json reference — dotnet/templating wiki](https://github.com/dotnet/templating/wiki/Reference-for-template.json)
- [Custom templates for dotnet new — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
- [How to create your own templates — .NET Blog](https://devblogs.microsoft.com/dotnet/how-to-create-your-own-templates-for-dotnet-new/)
- [Create a project template for dotnet new — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tutorials/cli-templates-create-project-template)

## Unresolved
- gRPC service file as conditional vs always-present (lean toward conditional `--grpc` flag deferred to v0.2 of template).
- Template versioning strategy when phase 04/05 building blocks evolve — bump template version, no auto-upgrade existing services.
