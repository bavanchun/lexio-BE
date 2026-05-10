# Phase 06 — docker-compose polyglot stack

## Context Links
- Doc §7.1 polyglot persistence strategy
- Doc §6.4.2-3 messaging
- Researcher B — full broker/db config research

## Overview
- Priority: P1
- Status: pending
- Brief: Single `docker compose up` brings up Postgres + Mongo + Redis + RabbitMQ + Kafka + Elasticsearch for local dev.

## Key Insights
- Compose project name `lexio` → containers prefixed `lexio-postgres`, etc. Set via top-level `name: lexio`.
- Single bridge network `lexio-net` for inter-container DNS.
- Postgres 6 DBs created via init script; only runs on first volume init.
- Kafka KRaft single-node combined mode (broker+controller); fixed `CLUSTER_ID` for dev.
- Elasticsearch single-node, security disabled — DEV ONLY, never prod (call out in runbook).

## Requirements
- Functional: `docker compose up -d` brings all services healthy in <60s on warm cache.
- NFR: total RAM target ≤4GB on developer workstations.

## Architecture
```
infra/
├── docker-compose.yml
├── .env.example
└── init/
    ├── postgres/
    │   └── 01-create-databases.sql
    └── rabbitmq/
        └── Dockerfile        (extends rabbitmq:3-management-alpine + delayed-message plugin)
```
Repo root carries a `docker-compose.yml` symlink OR put compose at root directly per user spec. **Decision: place at repo root** per user spec, and put init scripts under `infra/init/`.

## Related Code Files
Create:
- `/docker-compose.yml`
- `/.env.example`
- `/infra/init/postgres/01-create-databases.sql`
- `/infra/init/rabbitmq/Dockerfile`
- `/infra/init/rabbitmq/enabled_plugins`

## Implementation Steps
1. Branch `feat/be-compose` off `feat/be-bb-abstractions` (does not need impls).
2. Write `.env.example` at repo root:
   ```
   # DEV ONLY — never use in production
   POSTGRES_USER=lexio
   POSTGRES_PASSWORD=devpass
   POSTGRES_DB=postgres
   MONGO_INITDB_ROOT_USERNAME=lexio
   MONGO_INITDB_ROOT_PASSWORD=devpass
   RABBITMQ_DEFAULT_USER=lexio
   RABBITMQ_DEFAULT_PASS=devpass
   ELASTIC_PASSWORD=devpass
   KAFKA_CLUSTER_ID=MkU3OEVBNTcwNTJENDM2Qk  # fixed dev id
   ```
3. Write `infra/init/postgres/01-create-databases.sql`:
   ```sql
   CREATE DATABASE identity_db;
   CREATE DATABASE vocabulary_db;
   CREATE DATABASE learning_db;
   CREATE DATABASE statistics_db;
   CREATE DATABASE notifications_db;
   CREATE DATABASE social_db;
   ```
4. Write `infra/init/rabbitmq/Dockerfile`:
   ```dockerfile
   FROM rabbitmq:3-management-alpine
   RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
   ```
   And `enabled_plugins`:
   ```
   [rabbitmq_management,rabbitmq_delayed_message_exchange].
   ```
5. Write `docker-compose.yml`:
   ```yaml
   name: lexio
   services:
     postgres:
       image: postgres:17-alpine
       container_name: lexio-postgres
       environment:
         POSTGRES_USER: ${POSTGRES_USER:-lexio}
         POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-devpass}
         POSTGRES_DB: ${POSTGRES_DB:-postgres}
       ports: ["5432:5432"]
       volumes:
         - postgres-data:/var/lib/postgresql/data
         - ./infra/init/postgres:/docker-entrypoint-initdb.d:ro
       healthcheck:
         test: ["CMD-SHELL", "pg_isready -U $${POSTGRES_USER}"]
         interval: 5s
         timeout: 3s
         retries: 10
       networks: [lexio-net]

     mongo:
       image: mongo:8
       container_name: lexio-mongo
       environment:
         MONGO_INITDB_ROOT_USERNAME: ${MONGO_INITDB_ROOT_USERNAME:-lexio}
         MONGO_INITDB_ROOT_PASSWORD: ${MONGO_INITDB_ROOT_PASSWORD:-devpass}
       ports: ["27017:27017"]
       volumes: [mongo-data:/data/db]
       healthcheck:
         test: ["CMD", "mongosh", "--quiet", "--eval", "db.adminCommand('ping').ok"]
         interval: 10s
         retries: 6
       networks: [lexio-net]

     redis:
       image: redis:7-alpine
       container_name: lexio-redis
       command: ["redis-server", "--save", "", "--appendonly", "no"]
       ports: ["6379:6379"]
       healthcheck:
         test: ["CMD", "redis-cli", "ping"]
         interval: 5s
         retries: 6
       networks: [lexio-net]

     rabbitmq:
       build: ./infra/init/rabbitmq
       container_name: lexio-rabbitmq
       environment:
         RABBITMQ_DEFAULT_USER: ${RABBITMQ_DEFAULT_USER:-lexio}
         RABBITMQ_DEFAULT_PASS: ${RABBITMQ_DEFAULT_PASS:-devpass}
       ports: ["5672:5672", "15672:15672"]
       volumes: [rabbitmq-data:/var/lib/rabbitmq]
       healthcheck:
         test: ["CMD", "rabbitmq-diagnostics", "ping"]
         interval: 10s
         retries: 6
       networks: [lexio-net]

     kafka:
       image: apache/kafka:3.7.0
       container_name: lexio-kafka
       ports: ["9092:9092"]
       environment:
         KAFKA_NODE_ID: 1
         KAFKA_PROCESS_ROLES: broker,controller
         KAFKA_LISTENERS: PLAINTEXT://:9092,CONTROLLER://:9093
         KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092
         KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9093
         KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
         KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
         KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
         KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
         KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
         KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
         CLUSTER_ID: ${KAFKA_CLUSTER_ID:-MkU3OEVBNTcwNTJENDM2Qk}
       volumes: [kafka-data:/var/lib/kafka/data]
       healthcheck:
         test: ["CMD-SHELL", "/opt/kafka/bin/kafka-broker-api-versions.sh --bootstrap-server localhost:9092 || exit 1"]
         interval: 10s
         retries: 8
       networks: [lexio-net]

     elasticsearch:
       image: docker.elastic.co/elasticsearch/elasticsearch:8.15.0
       container_name: lexio-elasticsearch
       environment:
         discovery.type: single-node
         xpack.security.enabled: "false"
         ES_JAVA_OPTS: "-Xms512m -Xmx512m"
       ports: ["9200:9200"]
       ulimits:
         memlock: { soft: -1, hard: -1 }
         nofile:  { soft: 65536, hard: 65536 }
       volumes: [es-data:/usr/share/elasticsearch/data]
       healthcheck:
         test: ["CMD-SHELL", "curl -fs http://localhost:9200/_cluster/health || exit 1"]
         interval: 10s
         retries: 8
       networks: [lexio-net]

   volumes:
     postgres-data:
     mongo-data:
     rabbitmq-data:
     kafka-data:
     es-data:

   networks:
     lexio-net:
       driver: bridge
   ```
6. Verify: `cp .env.example .env && docker compose up -d && docker compose ps` — all services `healthy`.
7. Connectivity smoke:
   - `psql -h localhost -U lexio -d identity_db -c '\l'` lists 6 DBs.
   - `redis-cli -h localhost ping` → PONG.
   - `curl -s localhost:9200/_cluster/health | jq .status` ≠ red.
   - `curl -u lexio:devpass localhost:15672/api/overview` returns JSON.
   - `docker exec lexio-kafka /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 --list` exits 0.
8. `docker compose down -v` to clean.
9. Commit: `feat(be-infra): add docker-compose polyglot stack`.

## Todo List
- [ ] `.env.example` with dev creds + warning header
- [ ] Postgres init SQL with 6 DBs
- [ ] RabbitMQ Dockerfile + delayed-message plugin
- [ ] `docker-compose.yml` with 6 services + healthchecks + named volumes
- [ ] All services reach `healthy` on `up -d`
- [ ] Connectivity smoke for each
- [ ] Compose project name `lexio` (verified via `docker ps` prefix)

## Success Criteria
- `docker compose up -d` finishes < 60s warm; all services `healthy` within 2 min cold.
- Total memory < 4GB.
- All 6 Postgres DBs visible.
- Kafka topic creation works.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Kafka KRaft `CLUSTER_ID` mismatch on volume re-mount | M | H | Fixed dev `CLUSTER_ID` in `.env.example`; document `docker compose down -v` to reset |
| ES memlock fails on macOS Docker Desktop | M | M | Document workaround: comment out memlock ulimit on macOS, accept performance hit |
| Apple Silicon + apache/kafka image arch mismatch | M | M | apache/kafka multi-arch verified for arm64; if not, document `platform: linux/amd64` fallback |
| RabbitMQ delayed-message plugin version mismatch with rabbitmq:3 base | L | L | Pin base to `rabbitmq:3.13-management-alpine` |

## Security Considerations
- `.env.example` clearly marked DEV ONLY.
- ES security off → never apply this compose to staging/prod.
- Network is bridge-isolated; no ports beyond required exposed to host.
- Document: rotate Postgres/Mongo passwords for any non-localhost binding (defer to runbook in phase 12).

## Next Steps
Unblocks phase 07 (config strategy talks to these endpoints) and phase 09 (Testcontainers can use same image versions).
