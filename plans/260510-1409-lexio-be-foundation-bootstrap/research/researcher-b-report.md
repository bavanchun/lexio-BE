# Researcher B — Polyglot docker-compose stack 2026

## Postgres 17 multi-database init

- Image: `postgres:17-alpine`. Single instance, multiple logical DBs.
- Init script: `init/postgres/01-create-databases.sql` mounted at `/docker-entrypoint-initdb.d/`. Runs once on first volume init.
- SQL pattern: `CREATE DATABASE identity_db; CREATE DATABASE vocabulary_db;` etc. (6 DBs: identity, vocabulary, learning, statistics, notifications, social).
- Single superuser via `POSTGRES_USER=lexio` / `POSTGRES_PASSWORD=devpass` / `POSTGRES_DB=postgres` (initial). Each service owns its own DB but connects with same user in dev (split per-service users in prod).
- Healthcheck: `pg_isready -U lexio`.

## MongoDB 8

- Image: `mongo:8`. Vocabulary card-content store.
- For transactions support need replica set. Dev shortcut: `--replSet rs0` with one node + init script that runs `rs.initiate()`. If we don't need transactions in dev (cards are largely read-mostly + idempotent writes), run as standalone — simpler.
- **Decision:** standalone in dev, document that prod will run replica set.
- Volume: `mongo-data:/data/db`. Healthcheck: `mongosh --eval "db.adminCommand('ping')"`.

## Redis 7

- Image: `redis:7-alpine`. Persistence: AOF off in dev (faster). Volume optional.
- Healthcheck: `redis-cli ping`.

## RabbitMQ

- Image: `rabbitmq:3-management-alpine`. Ports: 5672 AMQP, 15672 mgmt UI.
- Default user/pass `guest/guest` only on localhost — set explicit `RABBITMQ_DEFAULT_USER=lexio` / `RABBITMQ_DEFAULT_PASS=devpass`.
- **Delayed messaging plugin** needed for scheduled notifications: enable via custom Dockerfile OR `definitions.json` mounted at `/etc/rabbitmq/`. Simplest: Dockerfile in `init/rabbitmq/Dockerfile` extending base + `rabbitmq-plugins enable rabbitmq_delayed_message_exchange`.
- Healthcheck: `rabbitmq-diagnostics ping`.

## Kafka 3.7 KRaft (no Zookeeper)

- Image: `apache/kafka:3.7.0` (official, not Confluent or Bitnami — official avoids licensing surprises).
- Single-node combined mode: `KAFKA_PROCESS_ROLES=broker,controller`.
- Required env:
  ```
  KAFKA_NODE_ID=1
  KAFKA_PROCESS_ROLES=broker,controller
  KAFKA_CONTROLLER_QUORUM_VOTERS=1@kafka:9093
  KAFKA_LISTENERS=PLAINTEXT://:9092,CONTROLLER://:9093
  KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://kafka:9092
  KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
  KAFKA_INTER_BROKER_LISTENER_NAME=PLAINTEXT
  KAFKA_CONTROLLER_LISTENER_NAMES=CONTROLLER
  KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1
  KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR=1
  KAFKA_TRANSACTION_STATE_LOG_MIN_ISR=1
  CLUSTER_ID=<base64 22-char> (or use kafka-storage random-uuid; for dev fix one)
  ```
- Healthcheck: `kafka-broker-api-versions --bootstrap-server localhost:9092`.
- Volume: `kafka-data:/var/lib/kafka/data`.

## Elasticsearch 8 single-node dev

- Image: `elasticsearch:8.15.0` (or whichever 8.x).
- Env: `discovery.type=single-node`, `xpack.security.enabled=false` (DEV ONLY — never prod), `ES_JAVA_OPTS=-Xms512m -Xmx512m`.
- Set `ulimits.memlock=-1` and `ulimits.nofile=65536`.
- Volume: `es-data:/usr/share/elasticsearch/data`. Port 9200.
- Healthcheck: `curl -f http://localhost:9200/_cluster/health || exit 1`.

## Compose conventions

- Use `name: lexio` at top level → containers prefixed `lexio-postgres`, `lexio-kafka`, etc.
- Single bridge network `lexio-net`.
- All services depend on healthchecks (`depends_on: { service: { condition: service_healthy } }`).
- `.env` at repo root; reference via `${POSTGRES_PASSWORD:-devpass}`.
- Compose v2 — top-level `version:` key omitted (deprecated).

## Sources
- [Apache Kafka Docker Hub](https://hub.docker.com/r/apache/kafka)
- [Kafka KRaft Docker tutorial — Instaclustr](https://www.instaclustr.com/education/apache-spark/running-apache-kafka-kraft-on-docker-tutorial-and-best-practices/)
- [Kafka 4 + KRaft + Docker Compose](https://medium.com/@kinneko-de/kafka-4-kraft-docker-compose-874d8f1ffd9b)
- [Kafka with Docker Compose 2026 — OneUptime](https://oneuptime.com/blog/post/2026-01-21-kafka-docker-compose/view)

## Unresolved
- Mongo standalone vs replica-set-of-one (decided standalone for v0; revisit if outbox/Mongo transactions needed).
- Pin exact ES 8 minor version vs `:8` rolling tag.
