# Deployment Guide

> **Canonical source:** `docs/Lexio_Complete_Documentation.docx` §9.
> This file covers deployment topology and process. K8s manifests land in a future phase.

## Environments

| Environment | Purpose | Trigger |
|-------------|---------|---------|
| Local | Development | `docker compose up -d` |
| Staging | Pre-production validation | Manual `cd.yml` dispatch |
| Production | Live | Manual `cd.yml` dispatch (after staging sign-off) |

## Container Strategy

Each service is built as a multi-stage Docker image (see `Lexio.ServiceTemplate/Lexio.Service1.Api/Dockerfile`):
1. `build` stage — SDK image, `dotnet publish -c Release`
2. `runtime` stage — `mcr.microsoft.com/dotnet/aspnet:10.0` minimal image

## CI/CD Pipeline

```
push/PR → ci.yml (build, format, test, arch, security, template-smoke)
             ↓ (main branch only)
         cd.yml (manual dispatch) → staging → production
```

`cd.yml` currently a stub (echo placeholder). Phase 3 will add:
- `docker build / docker push` to container registry.
- Kubernetes `kubectl rollout` or Helm upgrade.
- Smoke test against staging endpoint.

## Infrastructure Dependencies

All infrastructure runs in Docker for local dev. Production targets:
- PostgreSQL: managed (e.g., RDS, Supabase, or self-hosted PG 17).
- MongoDB: Atlas or self-hosted.
- Redis: Elasticache or self-hosted Redis 7.
- RabbitMQ: CloudAMQP or self-hosted 3.13.
- Kafka: Confluent Cloud or self-hosted KRaft cluster.
- Elasticsearch: Elastic Cloud or self-hosted 8.x.

## Secret Management

See [`docs/runbooks/configuration.md`](runbooks/configuration.md) for 6-layer config precedence
and Kubernetes sealed-secrets placeholder.

**Never commit secrets** — `.env` files are gitignored; use CI environment secrets for staging/prod.
