# Review rules for moq.analyzers

Bugbot reads this file on every pull request. Each rule names a defect to flag.
Repo facts live in `AGENTS.md` under "Repo facts agents must not miss". Check them before you flag a version, rule ID, or host claim.

## Prose

- Flag an em dash (U+2014) or an en dash (U+2013) in any added or changed line. This covers docs, skills, code comments, commit messages, and PR text. Use a comma, a colon, a period, or parentheses instead.
- Flag a reader doc (`README.md`, `docs/`) that links to `AGENTS.md` or depends on it for product facts. `AGENTS.md` is agent context.
- Flag a host requirement (SDK, Visual Studio, or Roslyn version) in a reader doc that does not name the releases it covers. Example: "Releases after 0.4.2 need the .NET 10 SDK."

## Cites in skills and docs

- Flag a changed file whose `file:line` cites in `.agents/skills/` or docs now point at the wrong line, or at a value that changed. The cite must move in the same pull request.
- Flag a cite of `.github/actionlint.yml`. The config file is `.github/actionlint.yaml`.

## Rule pages

- Flag a `docs/rules/` example that would not compile against real Moq, or that the analyzer would not flag as the page says.
- Flag any new use of the ID `Moq1003`. It is retired. The InternalsVisibleTo rule is `Moq1005`.

## Tooling

- Flag an `opengrep` entry in `.codacy/codacy.yaml`. The Codacy CLI must not pin opengrep.
- Flag a PowerShell call that passes a relative path to a .NET API such as `[System.IO.File]::ReadAllBytes`. .NET resolves relative paths against the process directory, not the PowerShell location. Resolve the path first with `Resolve-Path` or `Join-Path` on a known root.
- Flag a removal of `VSTEST_DISABLE_ARTIFACTS_DIRECTORY_VERIFICATION` from an ARM job in `.github/workflows/main.yml` that runs `dotnet test`. Without it, the .NET 11 test platform fails the step after every test passes.

## Performance

- Flag a performance job that fails on a screen result alone. ADR-012 needs paired confirmation rounds before a regression fails the job. One exception is correct: when round 1 shows that 3 rounds cannot fit the 40 minute budget, the screen result fails the job (ADR-012 item 3).
- Flag `DOTNET_ROLL_FORWARD=LatestMajor` in perf scripts or docs. The perf comparison uses `LatestMinor`.
