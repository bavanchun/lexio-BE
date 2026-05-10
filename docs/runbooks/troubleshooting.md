# Troubleshooting

Common issues encountered when setting up or running Lexio BE locally.

---

## Kafka: cluster ID mismatch on restart

**Symptom:** `org.apache.kafka.common.errors.InconsistentClusterIdException` in Kafka logs.

**Cause:** Kafka KRaft mode stores the cluster ID in the volume. If the container is recreated
without removing the volume, the ID in the volume conflicts with the new container.

**Fix:**
```bash
docker compose down -v   # removes volumes — destroys local data
docker compose up -d
```

---

## Port already in use

**Symptom:** `Error starting userland proxy: listen tcp 0.0.0.0:5432: bind: address already in use`

**Fix:**
```bash
# Find and kill the conflicting process
lsof -i :5432         # macOS / Linux
netstat -ano | findstr 5432   # Windows (WSL2: use lsof)
```

Or change the host port in `docker-compose.yml` (e.g., `"5433:5432"`).

---

## Elasticsearch: max virtual memory too low (Linux)

**Symptom:** `max virtual memory areas vm.max_map_count [65530] is too low`

**Fix (Linux):**
```bash
sudo sysctl -w vm.max_map_count=262144
# To persist: add to /etc/sysctl.conf
```

**Fix (macOS with Docker Desktop):** Set in Docker Desktop → Resources → Advanced → vm.max_map_count.

---

## dotnet SDK version mismatch

**Symptom:** `error NETSDK1045: The current .NET SDK does not support targeting .NET 10`

**Fix:** Install .NET 10 SDK from https://dotnet.microsoft.com/download and verify:
```bash
dotnet --version   # should output 10.x.x
```

If multiple SDK versions are installed, `global.json` pins the version:
```bash
dotnet --list-sdks   # see all installed
```

---

## Husky hooks not running on Windows

**Symptom:** Pre-commit hook does not execute; commits bypass format check.

**Cause:** Husky.Net hooks require a POSIX shell (`/usr/bin/env sh`). On Windows (non-WSL2),
`sh` may not be in PATH.

**Fix:** Use WSL2 for all development. Alternatively, install Git for Windows (includes `sh.exe`
in Git Bash, which `core.hooksPath` will invoke).

---

## dotnet format warning-as-error fails on new analyzer version

**Symptom:** `dotnet format --verify-no-changes` fails after pulling with a new analyzer message.

**Fix:**
1. Run `dotnet format` locally to auto-fix whitespace/style issues.
2. If a Sonar or Roslyn rule is intentionally suppressed, add the `#pragma warning disable` with
   a comment justifying the suppression.
3. Never suppress globally in `Directory.Build.props` without team discussion.

---

## NuGet restore: package not found

**Symptom:** `error NU1101: Unable to find package X`

**Cause:** Offline, corporate proxy, or missing NuGet source.

**Fix:**
```bash
dotnet nuget list source          # verify nuget.org is listed
dotnet restore --verbosity detailed
```

---

## RabbitMQ management UI not accessible

**Symptom:** http://localhost:15672 returns connection refused.

**Fix:** Ensure the `rabbitmq` container is healthy:
```bash
docker compose ps rabbitmq
docker compose logs rabbitmq | tail -30
```
Default credentials: `guest` / `guest` (set in `docker-compose.yml`).
