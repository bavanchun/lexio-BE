# Lexio.ServiceTemplate

Custom `dotnet new` template for Lexio microservices following Clean Architecture.

## Install

```bash
dotnet new install ./templates/Lexio.ServiceTemplate
```

## Use

```bash
# Via wrapper script (recommended — auto-adds to sln)
bash scripts/new-service.sh Identity

# Direct
dotnet new lexio-service -n Identity -o src/services/Identity
```

## Generated structure

```
src/services/Identity/
├── Lexio.Identity.Domain/          # Entities, AggregateRoots, ValueObjects, Domain Events
├── Lexio.Identity.Application/     # Commands/Queries, Behaviors, DTOs
├── Lexio.Identity.Infrastructure/  # EF Core, Redis, MassTransit impls
└── Lexio.Identity.Api/             # ASP.NET Core minimal API entry point + Dockerfile
tests/
├── Lexio.Identity.Domain.Tests/
├── Lexio.Identity.Application.Tests/
├── Lexio.Identity.Infrastructure.Tests/
└── Lexio.Identity.Api.Tests/       # Architecture fitness tests (NetArchTest)
```

## Uninstall

```bash
dotnet new uninstall Lexio.ServiceTemplate
```
