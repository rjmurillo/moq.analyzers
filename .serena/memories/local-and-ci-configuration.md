# Local and CI Configuration

## Git Hooks

Managed by [Husky.NET](https://github.com/alirezanet/Husky.Net) v0.9.1. Tasks defined in `.husky/task-runner.json`. Auto-installs on `dotnet restore` via MSBuild target in `Directory.Build.targets`. Requires PowerShell Core (`pwsh`).

### Pre-Commit (7 tasks)

All tools are required. Missing tools fail the hook (no soft-skip).

1. **Large file detection**: `build/scripts/hooks/Test-LargeFiles.ps1` (500 KB / 1000 lines)
2. **C# formatting**: `dotnet format --verify-no-changes` on staged `.cs` files
3. **Markdown linting**: `markdownlint-cli2` on staged `.md` files (lint only)
4. **YAML linting**: `yamllint` on staged `.yml`/`.yaml` files
5. **GitHub Actions linting**: `actionlint` on `.github/workflows/*.yml`
6. **JSON validation**: `build/scripts/hooks/Test-JsonValid.ps1` on staged `.json` files (excludes `.verified.json`)
7. **Shell linting**: `shellcheck` on staged `.sh`/`.bash` files

### Pre-Push (2 tasks)

1. **Tech debt scanner**: `Scan-TodoComments.ps1 -FailOnUnlinked`
2. **Build and test**: `build/scripts/hooks/Invoke-PrePushBuild.ps1` runs `dotnet build` (CI-parity flags) then `dotnet test`. Sets `DOTNET_ROLL_FORWARD=LatestMajor` for cross-TFM test execution.

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

- #1081: Pre-push hook parse error (fixed by Husky.NET migration and *.ps1 eol=lf in .gitattributes)
- #1084: Codacy rule files cause cascading file corruption (remove both files)
- #1085: Low-value AI editor rules and chatmodes to remove
