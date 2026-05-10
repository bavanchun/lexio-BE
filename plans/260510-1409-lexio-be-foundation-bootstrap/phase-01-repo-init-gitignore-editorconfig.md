# Phase 01 — Repo init: .gitignore, .gitattributes, .editorconfig

## Context Links
- Repo: `git@github.com:bavanchun/zlexio-BE.git` (verified: `git remote -v`)
- Plans: `plans/260510-1409-lexio-be-foundation-bootstrap/`
- FE precedent for git workflow + Conventional Commits

## Overview
- Priority: P1
- Status: pending
- Brief: Bring repo to a baseline state for .NET 10 development. No projects yet; just hygiene files.

## Key Insights
- Repo currently contains `LICENSE` and `.git` only; default branch `main`, 1 commit `55051ab`.
- `.NET 10 SDK` at `$HOME/.dotnet` — verify `dotnet --version` returns `10.0.203`.
- `.editorconfig` enforces formatting that `dotnet format` validates in CI.

## Requirements
- Functional: `.gitignore`, `.gitattributes`, `.editorconfig`, `LICENSE` retained.
- NFR: cross-platform line endings (LF), exclude all .NET artefacts and IDE noise.

## Architecture
N/A (config files only). Files at repo root.

## Related Code Files
Create:
- `/Users/vchun/Codes/My-projects/lexio-app/lexio-app-be/.gitignore`
- `/Users/vchun/Codes/My-projects/lexio-app/lexio-app-be/.gitattributes`
- `/Users/vchun/Codes/My-projects/lexio-app/lexio-app-be/.editorconfig`

Modify: none. Delete: none.

## Implementation Steps
1. `git checkout -b feat/be-foundation-init` from main.
2. Create `.gitignore` covering: `bin/`, `obj/`, `.vs/`, `.vscode/` (allow `.vscode/launch.json` if desired), `.idea/`, `*.user`, `*.suo`, `*.userosscache`, `*.sln.docstates`, `packages/`, `**/*.received.*`, `coverage/`, `TestResults/`, `*.coverage`, `*.coveragexml`, `artifacts/`, `appsettings.Local.json`, `appsettings.*.local.json`, `.env`, `.env.*` (except `.env.example`), `*.pfx`, `*.snk`, `node_modules/`, `.husky/_/`, `.DS_Store`, `Thumbs.db`. Use the standard GitHub VisualStudio template as base + project-specific additions.
3. Create `.gitattributes`:
   ```
   * text=auto eol=lf
   *.cs text diff=csharp
   *.csproj text merge=union
   *.sln text eol=crlf merge=union
   *.{png,jpg,jpeg,gif,ico,pdf,zip,nupkg} binary
   ```
4. Create `.editorconfig` with: `root = true`, indent_style=space, 4 spaces for `*.cs`, 2 spaces for `*.{json,yml,yaml,csproj,props,targets}`, end_of_line=lf, charset=utf-8, insert_final_newline=true, trim_trailing_whitespace=true. Add C# style rules: `csharp_new_line_before_open_brace = all`, `csharp_prefer_simple_using_statement = true:warning`, `dotnet_style_qualification_for_field = false:warning`, `dotnet_diagnostic.IDE0005.severity = warning` (remove unused usings). Defer extensive rule pack to phase 02 Directory.Build.props.
5. Save plans + research reports to git (`git add plans/` — already exist).
6. Commit: `chore(be-repo): add gitignore, gitattributes, editorconfig`. (Note: `chore` is normally banned in `.claude/` per project rule, but here applies to repo root non-claude config — allowed.)
7. Push branch + open stacked PR base = `main`.

## Todo List
- [ ] Verify `dotnet --version` == `10.0.203`
- [ ] Branch `feat/be-foundation-init` created
- [ ] `.gitignore` written with all .NET + IDE entries
- [ ] `.gitattributes` written with LF + binary entries
- [ ] `.editorconfig` written with C# + JSON/YAML rules
- [ ] `plans/` tracked in git
- [ ] Commit pushed, PR opened

## Success Criteria
- `git status` clean after `dotnet build` later phases (no accidental `bin/obj` tracked).
- `.editorconfig` parsed without errors by `dotnet format` (run sanity: `dotnet format whitespace --folder . --verify-no-changes` should be clean — but no .cs yet, so trivially passes).
- PR builds (no CI yet — manual verification).

## Risk Assessment
| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `.gitignore` missing an artefact, junk gets committed | M | M | Pull from canonical GitHub VisualStudio template |
| Mixed line endings on Windows devs | L | M | `* text=auto eol=lf` enforces LF on checkout |
| `.editorconfig` rules clash with later analyzers | L | L | Keep this phase's rules minimal; phase 02 adds the heavy stack |

## Security Considerations
- Ignore `.env`, `.env.*` (allow `.env.example` only). Ignore `appsettings.Local.json`. Prevents devpass leakage.
- Ignore `*.pfx`, `*.snk` (signing keys / cert files).

## Next Steps
Unblocks phase 02 (build props) — needs editorconfig + gitignore in place to validate clean builds.
