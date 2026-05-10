# Phase 10 — Husky.Net pre-commit + commit-msg hooks

## Context Links
- Phase 02 dotnet-tools manifest (Husky already installed)
- FE precedent: Conventional Commits enforced
- Doc-listed code quality tools

## Overview
- Priority: P2
- Status: pending
- Brief: Local enforcement before commit/push: dotnet format, secret scan, commit-message lint.

## Key Insights
- Husky.Net runs as dotnet tool; integrates with `core.hooksPath`.
- Pre-commit must be FAST (<5s on incremental) — only format staged `.cs`.
- Commit-msg validation: Node-based `@commitlint/cli` (well-trodden, FE already uses) OR pure-.NET regex via Husky task. Default Node-based.

## Requirements
- Functional:
  - `pre-commit` hook: `dotnet format whitespace --include $STAGED_CS --verify-no-changes` + `dotnet format style --include $STAGED_CS --verify-no-changes`.
  - `commit-msg` hook: validate Conventional Commits (regex: `^(feat|fix|chore|docs|refactor|test|perf|build|ci|revert)(\(([a-z0-9\-]+)\))?!?: .{1,100}$`).
  - `pre-push` hook (optional): `dotnet build /warnaserror`.
- NFR: total pre-commit time <5s on a 1-file change.

## Architecture
```
.husky/
├── task-runner.json      (Husky.Net config)
├── pre-commit            (shell script invoking dotnet husky run)
├── commit-msg            (shell script)
└── pre-push              (optional)

commitlint.config.js      (if Node path chosen)
package.json              (minimal — devDependencies for commitlint)
```

## Related Code Files
Create:
- `.husky/task-runner.json`
- `.husky/pre-commit`, `.husky/commit-msg`, `.husky/pre-push`
- `commitlint.config.js` (Node path) OR a pure-bash regex check (fallback)
- `package.json` minimal (Node path)

## Implementation Steps
1. Branch `feat/be-husky` off `feat/be-test-infra`.
2. `dotnet tool restore` to install Husky from manifest.
3. `dotnet husky install` — creates `.husky/` and sets `core.hooksPath`.
4. Configure `.husky/task-runner.json`:
   ```json
   {
     "tasks": [
       {
         "name": "dotnet-format-staged",
         "command": "dotnet",
         "args": ["format", "--include", "${staged}", "--verify-no-changes", "--no-restore"],
         "include": ["**/*.cs"]
       },
       {
         "name": "commitlint",
         "command": "npx",
         "args": ["--no-install", "commitlint", "--edit", "${args}"]
       }
     ]
   }
   ```
5. `.husky/pre-commit`:
   ```bash
   #!/usr/bin/env sh
   . "$(dirname "$0")/_/husky.sh"
   dotnet husky run --name dotnet-format-staged
   ```
6. `.husky/commit-msg`:
   ```bash
   #!/usr/bin/env sh
   . "$(dirname "$0")/_/husky.sh"
   dotnet husky run --name commitlint --args "$1"
   ```
7. `package.json`:
   ```json
   {
     "name": "lexio-be-tooling",
     "private": true,
     "devDependencies": {
       "@commitlint/cli": "^19.0.0",
       "@commitlint/config-conventional": "^19.0.0"
     }
   }
   ```
8. `commitlint.config.js`:
   ```js
   module.exports = {
     extends: ['@commitlint/config-conventional'],
     rules: {
       'scope-enum': [2, 'always', [
         'be-repo','be-build','be-shared','be-infra','be-config','be-template',
         'be-test','be-husky','be-ci','be-docs'
       ]],
       'subject-case': [0]
     }
   };
   ```
9. `npm install` to populate `node_modules`. `node_modules/` is already gitignored.
10. Smoke: `git commit -m "bad message"` fails. `git commit -m "feat(be-husky): wire pre-commit hooks"` passes.
11. Commit: `feat(be-husky): wire pre-commit and commit-msg hooks`.

## Todo List
- [ ] Husky installed + `core.hooksPath` set
- [ ] `task-runner.json` with format + commitlint tasks
- [ ] pre-commit + commit-msg hooks executable
- [ ] commitlint config restricts to Lexio scopes
- [ ] `package.json` + `node_modules/` (gitignored)
- [ ] Bad message rejected, good accepted
- [ ] dotnet format violation rejected

## Success Criteria
- Bad-format `.cs` cannot be committed.
- Non-conventional commit message rejected.
- Hook total time < 5s on incremental change.

## Risk Assessment
| Risk | L | I | Mitigation |
|---|---|---|---|
| Devs without Node can't commit | H | M | Document `npm i` step in `local-development.md`; provide pure-bash fallback if blocked |
| Hook bypass via `--no-verify` | H | L | Document team norm: never bypass; CI catches anyway |
| `dotnet format` slow on large staged sets | M | M | `${staged}` filters to staged only; full repo fmt in CI |

## Security Considerations
- Add optional `gitleaks` or `trufflehog` pre-commit step to block secret commits — defer to v0.2 if time-pressed.

## Next Steps
Unblocks phase 11 — CI runs same checks as fail-safe.
