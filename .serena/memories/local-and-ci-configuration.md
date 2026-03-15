# Local and CI Configuration

## Git Hooks

Located in `.githooks/`, shared via `LintHelpers.ps1`. Require PowerShell Core (`pwsh`).

### Pre-Commit (.githooks/hooks/Invoke-PreCommit.ps1)

Runs on staged files only, scoped by extension:

1. **Large file detection**: Max 500 KB file size, max 1000 lines for source files
2. **C# formatting**: `dotnet format` with auto-fix and re-stage (only for cleanly staged files)
3. **Markdown linting**: `markdownlint-cli2 --fix` on `.md` files only, with auto-fix and re-stage
4. **YAML linting**: `yamllint` on `.yml`/`.yaml` files (lint only, no auto-fix)
5. **GitHub Actions linting**: `actionlint` on `.github/workflows/*.yml` (lint only)
6. **JSON validation**: Python `json.tool` on `.json` files (excludes `.verified.json`)
7. **Shell linting**: `shellcheck` on `.sh`/`.bash` files (lint only)

Auto-fix safety: `Invoke-AutoFix` snapshots dirty files before fixing and only re-stages files that were clean before the fix ran.

### Pre-Push (.githooks/hooks/Invoke-PrePush.ps1)

1. **Tech debt scanner**: `Scan-TodoComments.ps1 -FailOnUnlinked` (BROKEN: #1081)
2. **Build**: `dotnet build` with CI-parity flags (Release, PedanticMode, Deterministic, ContinuousIntegrationBuild)
3. **Test**: `dotnet test` with runsettings (skipped if build fails)

## CI Workflows (.github/workflows/)

| Workflow | Trigger | Purpose |
|---|---|---|
| main.yml | push/PR to main | Build, test, package, coverage |
| linters.yml | push/PR | super-linter (YAML, Markdown, JSON, GitHub Actions, Bash) |
| release.yml | tag push | NuGet publish |
| release-drafter.yml | push to main | Auto-generate release notes |
| dependency-review.yml | PR | License and vulnerability check |
| devskim.yml | push/PR | Security scanning |
| powershell.yml | push/PR | PSScriptAnalyzer on .ps1 files |
| label-pr.yml | PR | Auto-label PRs by path |
| label-issues.yml | issue | Auto-label issues |
| semantic-pr-check.yml | PR | Conventional commit PR title check |
| copilot-setup-steps.yml | - | GitHub Copilot workspace setup |
| dependabot-approve-and-auto-merge.yml | PR | Auto-approve/merge dependency PRs |
| milestone-tracking.yml | PR merge | Track milestone progress |
| tech-debt-tracker.yml | schedule | Track TODO/FIXME/HACK markers |

## Linter Configurations

| File | Tool | Key Rules |
|---|---|---|
| .markdownlint.json | markdownlint-cli2 | MD013 off (line length), MD024 off (duplicate headings), MD033 off (inline HTML), MD041 off (first line heading), MD060 off |
| .yamllint.yml | yamllint | truthy check-keys off, line-length max 320, document-start disabled, 1 space min from content |
| actionlint.yml | actionlint | Custom self-hosted runner label: windows-2025-vs2026 |
| .editorconfig | dotnet format | Enforces formatting rules (tabs/spaces, line endings, naming) |

## Build Flags

| Flag | Where Defined | Purpose |
|---|---|---|
| PedanticMode=true | build/targets/codeanalysis/CodeAnalysis.targets | Enables warnings-as-errors and stricter analysis |
| Deterministic=true | Pre-push hook, CI | Reproducible builds |
| ContinuousIntegrationBuild=true | Pre-push hook, CI | Enables CI-specific behavior (SourceLink paths) |
| UseSharedCompilation=false | Pre-push hook | Disables shared Roslyn compiler for isolation |
| BuildInParallel=false | Pre-push hook | Single-threaded build for determinism |

## AI Editor Rules

Located in `.cursor/rules/`, `.windsurf/rules/`, `.github/chatmodes/`.

### HARMFUL (removal tracked in #1084, #1085)

- `.cursor/rules/codacy.mdc`: Auto-applies fixes after every edit, caused ~110 file corruption
- `src/tools/PerfDiff/.cursor/rules/codacy.mdc`: Same, with `alwaysApply: true` and empty globs
- `.github/chatmodes/Beast Mode.chatmode.md`: Reckless autonomous behavior, contradicts project philosophy
- `.cursor/rules/verify-information-rule.mdc`: Too vague, wastes context tokens
- `.cursor/rules/conventional-commit-messages.mdc`: Duplicates CI enforcement, wastes ~700 tokens

### SAFE

- `.cursor/rules/create-explainer.md`: PRD template, `alwaysApply: false`
- `.cursor/rules/process-task-list.md`: Task management process doc
- `.cursor/rules/csharp.mdc`: Redirect to copilot-instructions.md
- `.windsurf/rules/csharp.md`: Same redirect for Windsurf

## Known Broken Configurations

- #1081: Pre-push hook fails on Scan-TodoComments.ps1 parse error (workaround: `--no-verify`)
- #1084: Codacy rule files cause cascading file corruption (remove both files)
- #1085: Low-value AI editor rules and chatmodes to remove
