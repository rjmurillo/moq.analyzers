---
name: moq-analyzers-auto
description: "Default entry point and router for ALL work in the moq.analyzers repository — load this FIRST whenever a task arrives and no specialist skill has been chosen yet, when unsure which skill applies, or when starting any session in this repo. It classifies the task (bug fix, false positive/negative, new rule, code fix, perf, tests, environment, CI, dependency, release, docs, research, review), routes to an ordered chain of specialist skills with default commands and gates, and sets the auto-decision principles and hard STOP conditions for autonomous work. Do NOT load it when you already know exactly which specialist skill you need (go direct — e.g. moq-analyzers-debugging-playbook for a failing test, moq-analyzers-rule-lifecycle for a new rule); this skill contains routing and defaults only, not the facts themselves."
---

# Auto mode: classify, route, execute with defaults

You were probably handed a task, not a skill name. This skill turns the task into
an ordered plan using the 17 specialist skills, with defaults chosen so you do not
have to deliberate. It holds **routing and decision policy only** — every fact
(commands, thresholds, history, templates) lives in exactly one specialist skill,
and this router tells you which.

One rule overrides everything below: **never route around a gate.** If a default
here ever appears to conflict with `moq-analyzers-change-control`, change-control
wins.

## Phase 0 — Classify the task (30 seconds, no tools)

Match the request against this table. Take the FIRST row that matches. If two rows
match equally, the earlier row wins (they are ordered by risk).

| # | The task mentions / looks like | Intent |
|---|---|---|
| 1 | "crash", AD0001, CS8032, exception in analyzer, IDE broke | **Crash triage** |
| 2 | "false positive", "shouldn't warn", "flags valid code", "false negative", "misses", a MoqXXXX complaint, issues #1241–#1264/#1270 | **Correctness (FP/FN)** |
| 3 | A failing test, failing CI check, red build, hook rejection, "works locally but not CI" | **Failure diagnosis** |
| 4 | "new rule", "new analyzer", "detect X", "add Moq1XXX", "add a code fix" | **New rule / fixer** |
| 5 | "slow", "allocation", perf gate failure, benchmark, PerfDiff | **Performance** |
| 6 | "add tests", "coverage", "test for", improving an existing suite | **Test authoring** |
| 7 | Fresh clone, container setup, SDK errors, "can't build", tool restore | **Environment** |
| 8 | `.github/workflows`, renovate, dependabot, a version bump, `Directory.Packages.props` | **CI / dependency** |
| 9 | "release", "publish", "ship", version tag, AnalyzerReleases promotion | **Release** |
| 10 | `docs/rules`, README, ADR, commit message wording, release notes | **Docs** |
| 11 | "what should we build", "research", "corpus", "mutation testing", a new capability idea | **Research** |
| 12 | "review this PR/diff/change" | **Review** |

If nothing matches, treat it as **Correctness (FP/FN)** if it names a MoqXXXX rule,
otherwise ask the requester one clarifying question before proceeding.

## Phase 1 — Route (load skills in this order, then execute)

Each chain is ordered: read the first skill before acting, pull the later ones in
at the step where the chain says so. Do not load more than the chain lists —
over-loading wastes context; the specialists cross-reference what they need.

| Intent | Skill chain (in order) | First command to run | Gate before "done" |
|---|---|---|---|
| **Crash triage** | debugging-playbook → failure-archaeology (has this crashed before?) → architecture-contract (crash-safety invariants) | Reproduce: harness script from diagnostics-and-tooling on a minimal snippet | Regression test with issue link; full suite green; change-control evidence |
| **Correctness (FP/FN)** | **fp-convergence-campaign** (this IS its job) → it pulls debugging-playbook, moq-api-reference, proof-toolkit as needed | Campaign Phase 1: reproduce with the harness, BOTH Moq versions | Campaign Phase 4–5 (prove + promote); never skip a Moq version |
| **Failure diagnosis** | debugging-playbook (symptom table first) → build-and-env (if environmental) or validation-and-qa (if test-infra) | The symptom row's "first discriminating check" | Root cause named before any fix; span STOP protocol respected |
| **New rule / fixer** | rule-lifecycle (8-file checklist) → roslyn-analyzer-reference + moq-api-reference (while implementing) → validation-and-qa (test bar) → dotnet-api-design-standards (before review) | `grep -n "Moq1" src/Common/DiagnosticIds.cs` (pick a free ID in the right range) | All 8 checklist files touched; RS2000 clean; 100% block coverage; change-control evidence |
| **Performance** | diagnostics-and-tooling (measure FIRST) → architecture-contract (allocation invariants) → research-methodology (predict numbers before running) | `./build/scripts/perf/CIPerf.sh -filter '*(FileCount: 1)'` | Predicted vs. observed numbers in the PR; perf gate green (read the PerfDiff defect table in diagnostics-and-tooling before trusting it) |
| **Test authoring** | validation-and-qa → moq-api-reference (version fan-out facts) | Run the target suite: `dotnet test --settings ./build/targets/tests/test.runsettings --filter "FullyQualifiedName~<Class>"` | Positive + negative + doppelganger axes considered; spans pinned; both Moq versions |
| **Environment** | build-and-env (alone; it is self-contained) | Its top-of-file setup sequence | Green build AND green test run |
| **CI / dependency** | change-control (pins and who-may-change) → config-and-flags (what the knob does) → failure-archaeology (pin incidents: CS8032, S1135) | `git log --oneline -5 -- <file you are about to touch>` | actionlint + `gh act -n` evidence for workflows; NEVER bump a pinned dep without the superseding ADR |
| **Release** | rule-lifecycle (promotion runbook) → change-control (release gates) → docs-and-writing (notes) | Read `src/Analyzers/AnalyzerReleases.Unshipped.md` | Version-verify step; Shipped.md edited ONLY via promotion |
| **Docs** | docs-and-writing (templates + stale-docs list) | Its doc-inventory table (find the owner-of-truth) | markdownlint clean; doc-update triggers satisfied |
| **Research** | research-frontier (pick the problem) → research-methodology (design the experiment) → proof-toolkit (verify claims) | None — read first | Falsifiable milestone stated; claims labeled open/candidate |
| **Review** | change-control (what gates apply) → dotnet-api-design-standards (the quality bar) → validation-and-qa (is the evidence real?) | Read the diff before any skill deep-dive | Every behavior change has an issue-linked test; adversarial cases exist for AI-authored code |

## Phase 2 — The default operating loop (any change to `src/` or `tests/`)

Whatever the intent, a code change follows this loop. The router's defaults:

1. **Orient** — skim `moq-analyzers-architecture-contract` §invariants if touching
   `src/`. Skip only for docs/test-only changes.
2. **Reproduce / measure** — produce an observation BEFORE editing: harness output,
   failing test, benchmark number. If you cannot observe the problem, stop and say so.
3. **Change** — smallest edit that fixes the observed thing. Match surrounding style.
4. **Prove** — `dotnet format` → `dotnet build /p:PedanticMode=true` (CI-parity;
   plain `dotnet build` lies to you) → full
   `dotnet test --settings ./build/targets/tests/test.runsettings` → re-run the
   Phase-2 observation and show it changed. Claims you did not observe go through
   `moq-analyzers-proof-toolkit`.
5. **Gate** — assemble the PR evidence block per `moq-analyzers-change-control`.
   Conventional commit, squash merge, issue link.

## Decision policy (adapted from the project's doctrine)

Classify every decision you face into one of three kinds:

**Mechanical — decide silently.** One clearly right answer under these principles,
in priority order (this ordering is the project's own and is not negotiable):

| # | Principle | Default it produces |
|---|---|---|
| P1 | **Never crash the host** | When in doubt, bail out of analysis (no diagnostic) rather than risk a throw; guard every cast/index on syntax you did not verify |
| P2 | **No false positives or negatives** | A fix that silences a symptom without naming the mechanism is not a fix; test the wrapper axes (parens, extension methods, chains, delegates) |
| P3 | **Prove, don't assume** | Any claim about Moq's API, Roslyn's behavior, or an analyzer's output gets observed before it gets stated (proof-toolkit has the recipe) |
| P4 | **Measured, not eyeballed** | Perf and coverage statements come with numbers from the tools, predicted before running |
| P5 | **BCL-grade, smallest change** | Prefer the obvious 10-line fix matching existing patterns over a clever abstraction; reuse `src/Common` helpers |
| P6 | **Gates are law** | If a gate is inconvenient, satisfy it or escalate — never work around it |

**Taste — decide, then surface.** Reasonable people could disagree (naming, test
organization, message wording, where a helper lives). Pick per P5, state the choice
and the alternative in one sentence in the PR body, and move on. Do not block on it.

**STOP — never auto-decide.** These are the project's escalation tripwires. Halt
and ask a human (or the issue thread) instead of choosing:

- A diagnostic **span test fails twice** after one deliberate re-evaluation
  (the STOP protocol in debugging-playbook — trial-and-error is banned).
- You are **unsure which Roslyn API is correct** and no existing analyzer in
  `src/` demonstrates it.
- The fix seems to require **editing `AnalyzerReleases.Shipped.md`**, **bumping a
  pinned dependency** (Roslyn, AnalyzerUtilities, System.Collections.Immutable,
  Perfolizer, System.CommandLine), or **adding string-based type/method-name
  detection** — each has a superseding-ADR or hard-ban rule in change-control.
- You are tempted to **delete or disable a rule** to resolve complaints
  (failure-archaeology documents why that path is fenced).
- The task's **premise looks wrong** (the reported behavior is actually correct,
  the requested change contradicts an ADR). Say what you found; do not silently
  "fix" the premise.
- **Both your analysis and the evidence contradict the requester's stated
  direction.** Present: what they said, what you found, why, and the cost if you
  are wrong. Their call.

## Universal defaults (memorize these five)

Full command anatomy, flags, and traps live in build-and-env / diagnostics-and-tooling;
these are the five you will use in every session:

```bash
dotnet build /p:PedanticMode=true                                     # CI-parity build (plain build hides warnings)
dotnet test --settings ./build/targets/tests/test.runsettings          # full suite + coverage
dotnet format --verify-no-changes                                      # format gate
.claude/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh <file.cs> [4.8.2]   # "does this trigger MoqXXXX?"
git log --oneline -10 -- <path>                                        # has this file been fought over before?
```

And the four facts that prevent the most wasted time: warnings pass locally but
fail CI (PedanticMode); every analyzer needs Moq referenced or it silently does
nothing (`IsMockReferenced()` early-exit); test rows fan out across 2 namespaces ×
2 Moq versions (your one row runs 4 times); and `*.received.*` files are failure
artifacts — never commit them.

## When NOT to use this skill

- You already know the specialist you need — load it directly; this router adds
  nothing to a correctly-chosen chain.
- You need a fact (a threshold, a command flag, an incident, a template) — facts
  live in the specialist skills; if you find yourself quoting this file as a
  source for anything but routing and decision policy, you are citing the wrong
  document.
- The task is outside this repository — nothing here generalizes safely.

## Provenance and maintenance

- Routing table targets the 17 sibling skills present in `.claude/skills/` —
  re-verify the set: `ls .claude/skills/` (expect 18 dirs including this one).
  Last verified: 2026-07-02 against commit 0f40a36.
- Intent #2's issue range (#1241–#1264, #1270) is the 2026-07-02 correctness
  backlog — re-verify open states: `gh issue list -R rjmurillo/moq.analyzers --search "1241..1270"` or the GitHub UI.
- The five universal commands duplicate (deliberately, as memorization targets)
  facts owned by moq-analyzers-build-and-env and moq-analyzers-diagnostics-and-tooling —
  if a command changes there, change it here the same day.
- Decision-principle ordering mirrors the project priority list in
  `.github/copilot-instructions.md` ("Mission-Critical Quality Standard") — re-read
  that section if this file and it ever appear to disagree; it wins.
