# Phase 11 — GitHub Actions CI + dependabot + cd.yml stub

## Context Links
- Phase 09 `.runsettings` + `coverage.sh`
- Phase 10 hook scripts (CI mirrors)
- Phase 08 template smoke test

## Overview
- Priority: P1
- Status: pending
- Brief: Wire push/PR pipeline. Build, format-check, test, coverage, architecture, security, template-smoke. Deploy stub for later.

## Key Insights
- `actions/setup-dotnet@v4` with `dotnet-version: 10.0.x` — uses public dotnet feed.
- NuGet cache keyed on `**/packages.lock.json` SHA — requires `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` per project. Add to Directory.Build.props now (or lock just at root via `--use-lock-file` on restore).
- Coverage threshold enforcement via `dotnet test /p:Threshold=70 /p:ThresholdType=line` (Coverlet MSBuild integration), or fail script in `coverage.sh`.

## Requirements
- Functional jobs:
  - `build` — restore, build with `/warnaserror`, format verify
  - `test` — run tests, collect coverage, enforce ≥70% line on src/ assemblies
  - `architecture` — runs `Lexio.Architecture.Tests` separately for visibility
  - `security` — `dotnet list package --vulnerable --include-transitive` fails on any vuln; Trivy on Dockerfiles (template's Dockerfile)
  - `template-smoke` — `dotnet new install ./templates/Lexio.ServiceTemplate && dotnet new lexio-service -n Smoke -o /tmp/smoke && dotnet build /tmp/smoke`
- NFR: total wall time < 8 min on warm cache.

## Architecture
```
.github/
├── workflows/
│   ├── ci.yml
│   └── cd.yml             (stub, manual-trigger only)
├── dependabot.yml
└── CODEOWNERS              (optional placeholder)
```

## Related Code Files
Create:
- `.github/workflows/ci.yml`
- `.github/workflows/cd.yml`
- `.github/dependabot.yml`

## Implementation Steps
1. Branch `feat/be-ci` off `feat/be-husky`.
2. Write `.github/workflows/ci.yml`:
   ```yaml
   name: ci
   on:
     push: { branches: [main] }
     pull_request:
   concurrency:
     group: ci-${{ github.ref }}
     cancel-in-progress: true
   jobs:
     build-test:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with: { dotnet-version: '10.0.x' }
         - uses: actions/cache@v4
           with:
             path: ~/.nuget/packages
             key: nuget-${{ runner.os }}-${{ hashFiles('**/packages.lock.json','Directory.Packages.props') }}
         - run: dotnet tool restore
         - run: dotnet restore
         - run: dotnet format --verify-no-changes --no-restore
         - run: dotnet build --no-restore -warnaserror
         - run: dotnet test --no-build --collect:"XPlat Code Coverage" --settings .runsettings --results-directory TestResults
         - run: dotnet tool run reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:"Html;TextSummary;Cobertura"
         - name: enforce coverage
           run: |
             python3 - <<'PY'
             import xml.etree.ElementTree as ET, glob, sys
             files = glob.glob('coverage-report/Cobertura.xml')
             root = ET.parse(files[0]).getroot()
             rate = float(root.attrib['line-rate']) * 100
             print(f"line coverage: {rate:.2f}%")
             sys.exit(0 if rate >= 70 else 1)
             PY
         - uses: actions/upload-artifact@v4
           with: { name: coverage, path: coverage-report }
     architecture:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with: { dotnet-version: '10.0.x' }
         - run: dotnet test tests/architecture/Lexio.Architecture.Tests --no-restore || dotnet test tests/architecture/Lexio.Architecture.Tests
     security:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with: { dotnet-version: '10.0.x' }
         - run: dotnet restore
         - name: vulnerable packages
           run: |
             out=$(dotnet list package --vulnerable --include-transitive 2>&1 || true)
             echo "$out"
             echo "$out" | grep -q "has the following vulnerable packages" && exit 1 || true
         - uses: aquasecurity/trivy-action@master
           with:
             scan-type: config
             scan-ref: .
     template-smoke:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with: { dotnet-version: '10.0.x' }
         - run: dotnet new install ./templates/Lexio.ServiceTemplate
         - run: dotnet new lexio-service -n Smoke -o /tmp/smoke
         - run: dotnet build /tmp/smoke -warnaserror
   ```
3. Write `.github/workflows/cd.yml` stub:
   ```yaml
   name: cd
   on:
     workflow_dispatch:
       inputs:
         environment: { type: choice, options: [staging, production] }
   jobs:
     deploy:
       runs-on: ubuntu-latest
       steps:
         - run: echo "TODO Phase 3 — wire docker build/push + K8s rollout"
   ```
4. Write `.github/dependabot.yml`:
   ```yaml
   version: 2
   updates:
     - package-ecosystem: nuget
       directory: "/"
       schedule: { interval: weekly }
       groups:
         dotnet: { patterns: ["*"] }
     - package-ecosystem: github-actions
       directory: "/"
       schedule: { interval: weekly }
     - package-ecosystem: docker
       directory: "/"
       schedule: { interval: weekly }
     - package-ecosystem: npm
       directory: "/"
       schedule: { interval: weekly }
   ```
5. Push branch + open PR. Verify all jobs green on PR.
6. Commit: `feat(be-ci): add ci workflow, cd stub, dependabot`.

## Todo List
- [ ] `ci.yml` with 4 jobs (build-test, architecture, security, template-smoke)
- [ ] Coverage threshold ≥70% enforced
- [ ] NuGet cache keyed on lock + packages.props
- [ ] dotnet format verify-no-changes
- [ ] template-smoke generates + builds
- [ ] `cd.yml` stub manual-trigger only
- [ ] `dependabot.yml` for nuget + actions + docker + npm
- [ ] First PR builds green end-to-end

## Success Criteria
- PR check `ci / build-test` green.
- Architecture violations cause `architecture` job red (test by sample mutation, then revert).
- Vulnerable package detection works (mutation: introduce known-vuln older `Newtonsoft.Json` then revert).
- Template smoke regenerates without error.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| EF Core / OpenIddict pre-release packages flagged as vulnerable | M | M | Allowlist via `--source` filter or skip transitive on first pass |
| GH Actions Linux runner lacks Docker for Testcontainers | L | M | Default runner has Docker; verify; else add `services:` for Postgres |
| `dotnet format --verify-no-changes` flaps with new analyzers | M | M | Pin SonarAnalyzer minor version; document workflow to run `dotnet format` locally before pushing |
| Cache key over-invalidates on every Packages.props bump | M | L | Acceptable; coverage cache miss adds ~30s |

## Security Considerations
- Dependabot enabled across all ecosystems used.
- Trivy config-scan for misconfig in compose + Dockerfiles.
- `dotnet list package --vulnerable` fails build on any direct or transitive CVE.
- `cd.yml` stub explicitly NO secrets — won't accidentally leak when manually triggered.

## Next Steps
Unblocks phase 12 (docs reference CI badges).
