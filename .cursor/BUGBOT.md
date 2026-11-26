# Review rules for moq.analyzers

Bugbot reads this file on every pull request. Each rule names a defect to flag.
Repo facts live in `AGENTS.md` under "Repo facts agents must not miss". Check them before you flag a version, rule ID, or host claim.

## Prose

- Flag an em dash (U+2014) or an en dash (U+2013) in any added or changed line. This covers docs, skills, code comments, commit messages, and PR text. Use a comma, a colon, a period, or parentheses instead.

## Rule pages

- Flag a `docs/rules/` example that would not compile against real Moq. Also flag one that the analyzer would not report the way the page says. A flagged example must raise that rule's diagnostic, and a "Solution" example must raise none.

## Tooling

- Flag an `opengrep` entry in `.codacy/codacy.yaml`. The Codacy CLI must not pin opengrep. A local `.codacy/cli.sh analyze` run adds `opengrep@1.78.0` to that file, so do not commit the file after a local run.

## Performance

- Flag a performance job that fails on a screen result alone. ADR-012 needs paired confirmation rounds before a regression fails the job. One exception is correct: when round 1 shows that 3 rounds cannot fit the 40 minute budget, the screen result fails the job (ADR-012 item 3).
