---
name: moq-analyzers-failure-archaeology
description: Consult the chronicle of every major moq.analyzers investigation, dead end, rejected fix, and revert BEFORE starting work that resembles a past battle. Load it when a symptom looks familiar (false positive on a Moq rule, CS8032 analyzer-load failure, CI suddenly red on main, corrupted .git files, PerfDiff behaving oddly), when tempted to "just delete a noisy rule", "remove that string fallback", "raise a Sonar severity", or "fix CA1016", when evaluating an old copilot/fix-* branch, or when asked "has this been tried before?". This skill is memory, not method — do NOT load it for step-by-step debugging (moq-analyzers-debugging-playbook), the rules of what may change (moq-analyzers-change-control), architecture invariants (moq-analyzers-architecture-contract), or the active FP-fixing campaign playbook (moq-analyzers-fp-convergence-campaign).
---

# Failure archaeology: the moq.analyzers chronicle

This file is the project's institutional memory. Every entry is a battle that
was actually fought in this repository, with the commit hashes, pull request
(PR) and issue numbers that prove it, and the standing rule it produced.
Purpose: **nobody re-fights a settled battle, and nobody re-proposes a
rejected fix without new evidence.**

How to use it:

1. Match your symptom against the index below.
2. Read the matching entry. Follow its evidence trail (`git show <hash>`,
   issue numbers) before forming your own theory.
3. Obey the standing rule. If you believe a standing rule is wrong, that is a
   change-control question (see `moq-analyzers-change-control`), not a reason
   to quietly deviate.

Status legend:

| Status | Meaning |
|---|---|
| SETTLED | Fought, fixed, regression-tested, doctrine extracted. Do not re-litigate without new evidence. |
| ACTIVE | Open front. Check the linked issues' live state before touching the area — a full fix plan may already exist. |
| REJECTED | The approach was tried (or proposed) and turned down. The rejection is the finding. |

Terms used throughout (defined once):

- **FP / FN** — false positive (rule fires on correct code) / false negative
  (rule stays silent on broken code).
- **AD0001** — the diagnostic Roslyn (the .NET compiler platform) emits when
  an analyzer throws; the analyzer is then disabled for the session.
- **CS8032** — compiler warning "analyzer could not be loaded"; the analyzer
  silently does nothing for that consumer.
- **PedanticMode** — this repo's MSBuild property that turns on
  `TreatWarningsAsErrors`; it defaults to on in CI, off locally
  (`dotnet build /p:PedanticMode=true` reproduces CI strictness).
- **ADR** — Architecture Decision Record, under `docs/architecture/`.
- **MoqKnownSymbols** — `src/Common/WellKnown/MoqKnownSymbols.cs`, the
  per-compilation catalog of resolved Moq type/method symbols.

## Index

| # | Battle | Area | Status |
|---|---|---|---|
| 1 | String→symbol detection war | All analyzers | SETTLED |
| 2 | Moq1203 five-patch FP saga | Moq1203/1100/1206 | SETTLED (tail ACTIVE: #1243) |
| 3 | Moq1302 value-expression FPs | Moq1302 | SETTLED |
| 4 | S1135 TODO-warning CI self-lock | CI / .editorconfig | SETTLED |
| 5 | CS8032 host-compat break (v0.4.0→v0.4.1) | Packaging/deps | SETTLED |
| 6 | Codacy CA1016 — suppress, don't fix | External linting | SETTLED |
| 7 | Null-hardening cluster | src/Common + fixers | SETTLED |
| 8 | Per-operation allocation fixes | Perf | SETTLED |
| 9 | Missing AnalyzerUtilities.dll (v0.3.0-alpha) | Packaging | SETTLED |
| 10 | AI config-corruption incidents | Tooling/repo hygiene | ACTIVE |
| 11 | AI shared-blind-spot doctrine (Moq1302 origin) | Process | SETTLED |
| 12 | 2026-07-02 full audit (54 findings → #1241–#1278) | Everything | ACTIVE |

---

## 1. The string→symbol detection war (2024-10 → 2026-03)

- **Symptom:** analyzers that recognized Moq APIs by comparing method-name
  strings (`"Setup"`, `"Raises"`, `"Of"`) misfired on user code that happened
  to reuse those names, and missed real Moq calls the string list didn't cover.
- **Root cause:** string comparison was making *semantic* decisions. Moq's
  API surface (20+ `Returns` overloads, extension-method classes, interfaces
  like `IRaise<T>`) is too large and version-dependent to enumerate by name.
- **Evidence trail:**
  - `d25b4de` (2024-10-28, PR #245) — introduced the
    `WellKnownTypeProvider` + `KnownSymbols` pattern; the war's opening move.
  - PR #633 added a string fallback `IsLikelyMoqRaisesMethodByName` as an
    explicitly temporary safety net; issue #634 demanded its removal.
  - `5172cf3` (2025-10-16, PR #768) — **a documented failure**: removing the
    fallback broke 20 tests because symbol coverage was incomplete. Instead of
    forcing it, the PR *documented the blockers* and stopped.
  - `35d363d` (2025-11-12, PR #770) — the successful removal: first made
    symbol detection comprehensive (including `IRaise<T>`), then deleted the
    fallback.
  - `a974999` (2026-03-08, PR #1030) — removed the last string fallbacks
    (delegate-type checks in `EventSyntaxExtensions`, `"Of"` comparison in
    `LinqToMocksExpressionShouldBeValidAnalyzer`).
- **Status:** SETTLED — codified as ADR-001
  (`docs/architecture/ADR-001-symbol-based-detection-over-string-matching.md`).
- **Standing rules:**
  - Symbol-based detection only. A cheap name pre-filter is allowed *only* as
    a performance fast path in front of an authoritative symbol check.
  - Never remove a fallback without first proving symbol coverage is complete
    (run the full suite with the fallback deleted; 20 red tests = not yet).
  - A documented failed attempt (like `5172cf3`) is a legitimate, mergeable
    deliverable. Write the failure down; it saved PR #770 from repeating it.

## 2. The Moq1203 five-patch FP saga (2026-02 → 2026-05)

Moq1203 = "Method setup should specify a return value". The single most
instructive FP story in the repo: five separate patches, each fixing a
syntactic *wrapper* the previous fix didn't anticipate.

- **Symptom:** repeated user-visible FP reports on setups that clearly did
  specify a return value — just not in the shape the analyzer's chain-walker
  expected.
- **Root cause (recurring):** the walker matched one concrete syntax shape;
  every real-world wrapping of the same semantics (chained call, parentheses,
  delegate overload, extension method) evaded it.
- **The five patches, in order:**

| Patch | Commit | Date | PR | Fixes | Wrapper that was missed |
|---|---|---|---|---|---|
| 1 | `6ec810c` | 2026-02-15 | #886 | #849 | `.Callback(...)` chained *before* `.Returns(...)`; `ReturnsAsync`/`ThrowsAsync` not recognized; moved detection to symbols |
| 2 | `c270302` | 2026-02-16 | #895 | #887 | `(mock.Setup(...))` wrapped in parentheses |
| 3 | `894313b` | 2026-02-20 | #907 | — | Same parentheses gap existed in Moq1100 and Moq1206; added shared `WalkDownParentheses` |
| 4 | `0bef80b` | 2026-02-20 | #919 | #910, #911 | Delegate-based overloads: `GetSymbolInfo` on the member access lacks argument context — must query the parent invocation. Added a name-based fallback (see tail below) |
| 5 | `5eec7e1` | 2026-05-07 | #1086 | #1067 | Extension methods wrapping the setup; added `ImplementsMoqFluentInterface` |

- **The tail (ACTIVE):** the 2026-07-02 audit found patch 4's name fallback
  fires even when the symbol *resolved* to a non-Moq method — an ADR-001
  violation producing FNs. Open issue **#1243** carries the full fix plan:
  track `Moq.GeneratedReturnsExtensions` in `MoqKnownSymbols` (the untracked
  class that motivated the fallback) and gate the fallback on
  `Symbol is null && CandidateSymbols.IsEmpty`.
- **A nearby rejection, often misremembered:** branch `copilot/fix-496`
  (never merged) proposed a NEW `InSequenceSetupShouldBeProperlyConfiguredAnalyzer`
  under the then-unused ID Moq1203 (branch commit `eba89f2`, 2025-06-18), then
  removed its own proposal (branch commit `8037857`) after the maintainer's
  refute-by-construction on PR #504 ("no real value for this analyzer" — see
  moq-analyzers-research-methodology §"Refutation also applies to proposals").
  The shipped `MethodSetupShouldSpecifyReturnValue` rule — this saga's subject —
  only took over the Moq1203 ID afterwards, via #514 (`97d2714`, 2025-06-20);
  it was never a deletion target. The saga's own record shows the project's
  answer to a noisy shipped rule: harden it (five times), don't delete it.
- **Status:** SETTLED for the five FPs (each has an issue-linked regression
  suite); ACTIVE for #1243.
- **Standing rule — the wrapper axis is a mandatory test dimension.** Any
  rule that walks a fluent chain must have test rows for, at minimum:
  parentheses (single and nested), interposed `Callback`, delegate-based
  overloads, extension-method wrappers, and multi-link chains. Five patches
  is what skipping that axis costs.

## 3. Moq1302 value-expression FPs (2026-03)

Moq1302 = "LINQ to Mocks expression should be valid" (validates
`Mock.Of<T>(...)` lambdas).

- **Symptom (issue #1010, filed by an external user 2026-03-06):**
  `Mock.Of<Response>(static r => r.Status == StatusCodes.Status200OK)` was
  flagged — the analyzer reported the *constant on the right-hand side* as an
  "invalid member in LINQ to Mocks expression". The code compiles and runs.
- **Root cause:** the expression walker validated every member reference in
  the lambda, including **value expressions** — the right-hand side of `==`,
  static members, constants — which are inputs to the comparison, not members
  being mocked.
- **Evidence:** fix `4b705e2` (2026-03-06, PR #1017) filters
  non-lambda-rooted members; `3399297` (2026-03-07, PR #1020) added the
  comprehensive regression suite. Both in milestone v0.4.2.
- **Status:** SETTLED.
- **Standing rules:**
  - Walker discipline: (1) register on the operation kind, (2) guard
    `operation.Instance == null` (static receiver = value expression → skip),
    (3) only then validate the member.
  - Only expressions rooted at the lambda parameter are mock-target
    expressions; everything else is a value expression and out of scope.

## 4. The S1135 TODO-warning CI self-lock (2026-03-06/07)

S1135 is SonarAnalyzer's "complete the task associated to this TODO comment".

- **Symptom:** CI broke immediately after tech-debt tracking landed — the
  repo's own code failed the build.
- **Root cause chain:** `3d4f7ff` (2026-03-06) added the TODO/FIXME scanner
  *and* raised S1135 from suggestion to warning. Under PedanticMode
  (`TreatWarningsAsErrors`, on in CI), the codebase's existing, perfectly
  legal issue-linked `TODO(#1012)` comment became a build **error**. The
  tracking feature locked out the thing it was tracking.
- **Evidence:** reverted the next day by `b1439ab` (2026-03-07) — a one-line
  `.editorconfig` change back to suggestion.
- **Status:** SETTLED.
- **Standing rules:**
  - Never raise S1135 above `suggestion` while PedanticMode exists.
  - TODO discipline is enforced by the scanner
    (`build/scripts/todo-scanner/Scan-TodoComments.ps1`, pre-push +
    `tech-debt-tracker.yml`), which requires the `TODO(#123)` issue-linked
    format — not by compiler severity.
  - General lesson: before promoting any diagnostic to warning, grep the repo
    for existing instances; under PedanticMode every promotion is a
    potential self-lock.

## 5. The CS8032 host-compat break (v0.4.0 → v0.4.1, 2026-02)

- **Symptom:** after release v0.4.0, consumers building with the .NET 8 SDK
  saw CS8032 — the analyzers silently stopped running in their builds.
- **Root cause:** the analyzer package pulled a dependency chain
  (AnalyzerUtilities 4.x → `System.Collections.Immutable` 9.x) that cannot
  load in a .NET 8 host process. The analyzer DLL loads inside the
  *consumer's* compiler, so the consumer's runtime — not this repo's — sets
  the ceiling.
- **Evidence:** issue #850; fix `38943ac` (2026-02-16, PR #888), shipped in
  tag **v0.4.1** (cherry-picked to the release branch as `9c4184f`) — **a
  release that existed solely to undo this**. Codified as
  ADR-003 (Roslyn pinned to 4.8) and ADR-004 (AnalyzerUtilities capped at
  3.3.4). Renovate caps added in `dac582f` (2026-03-14, PR #1072).
- **Now triple-enforced:** the `ValidateAnalyzerHostCompatibility` MSBuild
  target, an inline CI DLL-reference check, and the 9-way
  `analyzer-load-test` CI matrix (net8/9/10 CLI × net472/48/481 MSBuild).
- **Status:** SETTLED.
- **Standing rule:** never bump `Microsoft.CodeAnalysis.*`, AnalyzerUtilities,
  `System.Collections.Immutable`, or `System.Reflection.Metadata` outside the
  ADR-003/ADR-004 process; the load-test matrix must pass. Full gate list in
  `moq-analyzers-change-control`.

## 6. Codacy CA1016: suppress, don't fix (2026-02/03)

CA1016 = "Mark assemblies with AssemblyVersionAttribute". Codacy is the
cloud static-analysis service wired into PRs.

- **Symptom:** Codacy repeatedly flagged CA1016 on this repo's assemblies.
- **Root cause:** it is a tool artifact, not a code defect. Nerdbank
  .GitVersioning generates `AssemblyVersion` **during the MSBuild build**;
  Codacy compiles files *outside* the MSBuild context and cannot see the
  generated attribute.
- **Evidence:** `58924f7` (2026-02-21, PR #925) suppressed it in
  `tests/.editorconfig`; `2a7ee34` (2026-03-08, PR #1051) suppressed it with
  severity `none` in the root `.editorconfig`.
- **Status:** SETTLED.
- **Standing rule:** do NOT "fix" CA1016 by hand-writing
  `AssemblyVersionAttribute` — NBGV owns versioning, and a manual attribute
  would conflict with it. The suppression is the correct end state. Treat any
  future Codacy CA1016 report as noise.

## 7. The null-hardening cluster (2026-03)

- **Symptom:** a review wave found latent crash and correctness risks:
  reference equality used on Roslyn symbols, null-forgiving `!` applied to
  values that can legitimately be null mid-edit, unguarded nulls in code-fix
  registration.
- **Evidence (all merged within four days):**
  - `ffed678` (2026-03-05, #997) — use `SymbolEqualityComparer` for all
    symbol comparisons (plain `==` on `ISymbol` is unreliable).
  - `f9ec6ca` (2026-03-06, #998) — replace null-forgiving `!` with real
    guards.
  - `c61a66a` (2026-03-06, #1000) — treat *unresolvable* parameter types as a
    mismatch in callback validation rather than guessing.
  - `f0161a7` (#1004) — guard code-fix registration when prerequisites are
    null.
  - `4052954` (2026-03-08, #1027) — `ArgumentNullException` for null source
    in `EnumerableExtensions.DefaultIfNotSingle`.
- **Status:** SETTLED; now doctrine in `.github/copilot-instructions.md`.
- **Standing rules:** `SymbolEqualityComparer` always; `!` is banned on any
  value that is not provably non-null; when the semantic model cannot resolve
  something mid-edit, the analyzer says "cannot verify" (usually: stay
  silent), never "assume".

## 8. Per-operation allocation fixes (2026-03)

- **Symptom:** per-keystroke performance cost: `MoqKnownSymbols` (which
  resolves dozens of Moq symbols) was being constructed per *operation*
  callback instead of once per compilation; hot paths allocated arrays before
  knowing whether a diagnostic would even be reported.
- **Evidence:** `9febdda` (2026-03-07, #1026) — create `MoqKnownSymbols` per
  compilation, not per operation; `3b5ac71` (2026-03-07, #1033) — hoist it in
  9 more analyzers; `7595080` (2026-03-08, #1050) — eliminate array
  allocations in constructor-argument matching (the `FilteredArgumentList`
  design).
- **Status:** SETTLED — the lifecycle rule is ADR-006.
- **Standing rules:** one `MoqKnownSymbols` per
  `CompilationStartAnalysisContext`, passed down to callbacks; the common
  no-diagnostic path must not allocate; expensive strings
  (`ToDisplayString()`) are computed only on the report path. (The 2026-07-02
  audit found remaining violations of the last point — see entry 12,
  issues #1259–#1261.)

## 9. Missing AnalyzerUtilities.dll (v0.3.0-alpha, 2025-01)

- **Symptom:** the v0.3.0-alpha package crashed consumers' IDEs — the
  analyzer assembly could not load its helper dependency.
- **Root cause:** `Microsoft.CodeAnalysis.AnalyzerUtilities.dll` (a
  `PrivateAssets="all"` dependency that must be *bundled*, since analyzer
  packages cannot express runtime NuGet dependencies) was not packed into the
  nupkg.
- **Evidence:** fix `5f4914b` (2025-01-17, PR #326 "Add AnalyzerUtilities to
  NuGet package"), first contained in tag `v0.3.0-alpha.1` — the alpha
  channel absorbed the damage before any stable release. The pack line lives
  today at `src/Analyzers/Moq.Analyzers.csproj` (the
  `<None Include="$(OutputPath)\Microsoft.CodeAnalysis.AnalyzerUtilities.dll" Pack="true" ...>`
  item).
- **Status:** SETTLED.
- **Standing rules:** package contents are pinned by Verify.Xunit snapshot
  tests (`PackageTests` in `tests/Moq.Analyzers.Test/`); any packaging change
  must update the `.verified.` snapshots deliberately. Ship risky changes
  through a pre-release (alpha) first — it demonstrably works.

## 10. AI config-corruption incidents (2026-03-15, ACTIVE)

Four issues, all split from #1080, all **open** as of 2026-07-02. If git or
the repo suddenly looks insane, check here before debugging.

| Issue | Symptom | Root cause | Fix/workaround |
|---|---|---|---|
| #1082 | `git` fails: `fatal: protocol '...` — `.git/config` remote URL wrapped in `<angle brackets>` | Agent-generated shell commands embedding markdown autolink syntax in `git remote set-url`; recurs each session | `git remote set-url origin https://github.com/rjmurillo/moq.analyzers` |
| #1083 | `fatal: unexpected line in .git/packed-refs` — blank line after header at session start | Suspected LSP/session-init git operation | `grep -v '^$' .git/packed-refs > /tmp/pr.tmp && mv /tmp/pr.tmp .git/packed-refs` |
| #1084 | ~110 files corrupted: `#pragma` → `# pragma`, URLs wrapped in `<...>` inside C# strings, YAML indentation destroyed | `.cursor/rules/codacy.mdc` (and a PerfDiff copy) instructed agents to auto-apply "fixes" after *every* edit — unbounded cascade | Remove both rule files (proposed in the issue) |
| #1085 | Context waste + reckless-autonomy risk from low-value AI editor rules/chatmodes | `alwaysApply: true` rules duplicating CI enforcement; a chatmode instructing "keep going until solved" | Remove listed files (proposed in the issue) |

- **Status:** ACTIVE — re-check issue state before assuming fixed.
- **Standing rules:**
  - Never add an AI-editor rule that auto-applies fixes after edits. The
    ~110-file cascade is the proof.
  - Write plain URLs in shell commands — never markdown autolinks (`<...>`).
  - When git errors match the table above, apply the workaround and move on;
    do not burn a session debugging git itself.

## 11. AI shared-blind-spot doctrine (Moq1302 origin story)

- **What happened:** Moq1302 was authored end-to-end by an AI agent — PR #511
  (merged 2025-06-25, milestone v0.4.0) contained the analyzer, its tests,
  and its docs. Implementation and tests were written by the same mind, so
  they shared a blind spot: neither considered value expressions (entry 3).
  The gap surfaced as FP #1010 eight months later, filed by an external user
  from a real production codebase.
- **Status:** SETTLED doctrine (process rule, not code).
- **Standing rule:** AI-written implementation plus AI-written tests do not
  check each other. Before merging any AI-authored rule or fix, a human adds
  adversarial boundary cases the implementation was *not* written against —
  at minimum: literal, local variable, static member, `const`, and
  method-call operands in every expression position the rule inspects.

## 12. The 2026-07-02 full audit (newest entry, ACTIVE)

A line-by-line read-only audit of `src/`, PerfDiff, workflows, and sampled
tests, completed 2026-07-02 against commit `05135b2`.

- **Findings:** 54 total — 1 Critical, 6 High, 16 Medium, 17 Low, 14 Info.
- **Disposition:** all filed the same day as **32 implementation-ready
  issues #1241–#1278**, each containing a complete, code-level fix plan
  verified against the built DLLs. Six accidental duplicates were closed
  immediately (#1244, #1246, #1247, #1249, #1252, #1254 — filing-race
  artifact; e.g. #1244 duplicates #1241).
- **Headliners:**
  - **#1241 (the 1 Critical):** `ConstructorArgumentsShouldMatchAnalyzer`
    does an unguarded `(IArrayTypeSymbol)` cast; a C# 13 params collection
    (`params ReadOnlySpan<T>`) on a modern Roslyn host →
    `InvalidCastException` → AD0001. Note the issue's correction of the naive
    fix: bail with `return true` (no diagnostic), because `return false`
    converts the crash into a Moq1002 FP.
  - **PerfDiff gate-integrity cluster #1265–#1269:** the perf merge gate has
    holes — #1265: the ETL comparison *never sets* its regression verdict, so
    the moment ETL traces appear in CI, any real BenchmarkDotNet regression
    is dismissed as "noise" (exit 0, gate silently disarmed); plus silently
    intersected benchmark sets (a PR that breaks the harness passes green),
    baseline-blind absolute-budget strategies, and infinite-ratio exclusion.
  - **Canonical-pattern FP:** `mock.Raise(m => m.Closed += null,
    EventArgs.Empty)` on a non-generic `EventHandler` event is flagged
    Moq1202/Moq1204 — Moq's own documented usage.
  - **Code-fix crash:** the Moq1100 Callback lightbulb throws on mid-edit
    `mock.Setup().Callback(...)` (unguarded `Arguments[0]`).
  - **#1243:** the Moq1203 name-fallback tail (entry 2).
- **Issue-range map:** crash/correctness #1241–#1258 and #1262–#1264; perf
  #1259–#1261; PerfDiff #1265–#1269; test coverage #1270; CI security
  #1271–#1272; maintainability #1273–#1278.
- **Status:** ACTIVE — this is the first campaign inside the maintainer's
  declared top priority: making FP-fixing *converge* (see
  `moq-analyzers-fp-convergence-campaign`).
- **Standing rule:** before writing any fix in these areas, read the matching
  issue — the implementation plan, tests, and constraints are already written
  and reviewed. Duplicating that work, or fixing it a *different* way without
  rebutting the issue's plan, wastes the audit.

---

## Dead-branch table

Unmerged remote branches are idea archives, not sources of truth. Merge
status verified with `git branch -r --no-merged main` on 2026-07-02.

| Branch | What it is | Verdict | Where the idea lives now |
|---|---|---|---|
| `copilot/fix-496` | AI spike for issue #496 that proposed a new `InSequence` analyzer under the then-unused ID Moq1203 (`eba89f2`), then **removed its own proposal** (branch commit `8037857`) after the maintainer's refute-by-construction on PR #504 | REJECTED (retired in writing on PR #504), never merged | The Moq1203 ID was later assigned to the shipped `MethodSetupShouldSpecifyReturnValue` rule (#514, `97d2714`) — entry 2's saga rule was never the deletion target. Sequence-pattern work was scoped in #576/#614–#617; #614–#617 were closed as stale 2026-05-09 without implementation — see moq-analyzers-research-frontier Problem 3 for the revival path |
| `copilot/fix-634` | AI spike at removing the Raises string fallback | Superseded, never merged | Landed via the human-completed PR #770 (`35d363d`, entry 1) |
| `copilot/fix-85`, `fix-417`, `fix-434`, `fix-436`, `fix-505`, `fix-695`, `copilot/assess-code-coverage-gaps` | Other AI spikes | Unmerged; unreviewed | Cross-check main before reusing anything from them — the pattern is that ideas land later via reviewed PRs |
| `copilot/fix-524` | The one merged `copilot/fix-*` | Landed | main |
| `ci/add-docs-build` | docfx documentation-website build + rule-doc CI artifacts | Abandoned, unmerged — **idea still relevant** | Unscheduled; do not treat as a dead concept |
| `feature/build-enable-cache` | NuGet caching + locked-mode restore in `main.yml` | Abandoned, unmerged — **idea still relevant** | Unscheduled |

## Do-not-refight one-liners

- Do not add string-name semantic checks; do not remove a fallback without
  symbol-coverage proof (entries 1, 2).
- Do not delete a noisy rule; fix it — the Moq1203 FP saga was resolved by
  five targeted hardening patches, never by deletion. (Do not misread
  `copilot/fix-496`: its `8037857` removed the branch's OWN proposed
  InSequence analyzer, which then held the unused Moq1203 ID, after PR #504's
  refutation — not the shipped rule.)
- Do not ship a chain-walking rule without wrapper-axis tests (parentheses,
  Callback interposition, delegate overloads, extension wrappers).
- Do not validate value expressions (comparison RHS, statics, constants) as
  mock targets in expression-tree rules.
- Do not raise S1135 (or promote any diagnostic to warning) without grepping
  for existing instances first — PedanticMode makes it a CI self-lock.
- Do not bump Roslyn/AnalyzerUtilities/SCI/SRM outside ADR-003/ADR-004 —
  v0.4.1 exists because someone effectively did.
- Do not "fix" Codacy's CA1016 by adding `AssemblyVersionAttribute`; the
  suppression is the fix.
- Do not use `==` on `ISymbol`, `!` on uncertain values, or "assume it
  matches" on unresolvable mid-edit code.
- Do not construct `MoqKnownSymbols` anywhere but compilation start.
- Do not add AI-editor rules that auto-apply fixes after edits.
- Do not merge an AI-authored rule whose only tests were AI-authored with it.
- Do not start a fix in an audit area (#1241–#1278) without reading the
  issue's existing implementation plan.

## When NOT to use this skill

This skill answers "what happened before and what rule did it produce". For
anything else, load the sibling:

| Need | Sibling skill |
|---|---|
| Gates, evidence requirements, what may be edited, release promotion | `moq-analyzers-change-control` |
| Step-by-step diagnosis of a misbehaving analyzer right now | `moq-analyzers-debugging-playbook` |
| The invariants themselves (ADR contents, thread-safety, lifecycle) | `moq-analyzers-architecture-contract` |
| Roslyn API concepts (symbols, operations, contexts) | `roslyn-analyzer-reference` |
| Moq API semantics (overloads, fluent chain, It/ItExpr) | `moq-api-reference` |
| Build/test/format commands, SDK setup, environment traps | `moq-analyzers-build-and-env` |
| .editorconfig, severities, PedanticMode mechanics | `moq-analyzers-config-and-flags` |
| Authoring a new rule end to end | `moq-analyzers-rule-lifecycle` |
| Test-writing standards, markup, verifiers | `moq-analyzers-validation-and-qa` |
| BCL/API design standards for public surface | `dotnet-api-design-standards` |
| PerfDiff/benchmark tooling operation | `moq-analyzers-diagnostics-and-tooling` |
| Rule docs / writing style | `moq-analyzers-docs-and-writing` |
| Running the active FP-convergence campaign (#1241–#1278 execution) | `moq-analyzers-fp-convergence-campaign` |
| Proving zero-FP claims on corpora | `moq-analyzers-proof-toolkit` |
| Unstarted/experimental ideas (sequence epic, mutation testing) | `moq-analyzers-research-frontier`, `moq-analyzers-research-methodology` |

## Provenance and maintenance

Re-verify before trusting anything volatile in this file:

- All cited commits exist with these subjects/dates:
  `for h in d25b4de 5172cf3 35d363d a974999 6ec810c c270302 894313b 0bef80b 5eec7e1 4b705e2 3399297 3d4f7ff b1439ab 38943ac 5f4914b ffed678 f9ec6ca c61a66a f0161a7 4052954 9febdda 3b5ac71 7595080 58924f7 2a7ee34 dac582f; do git show -s --format='%h %ad %s' --date=short $h; done`
  (These span the full project history. In a shallow clone — CI checkouts and
  some fresh clones — old hashes resolve as "not found" / "bad object"; run
  `git fetch --unshallow` first. Every hash is reachable in a full clone,
  verified 2026-07-04.)
- ACTIVE entries still active (issue states): check #1082–#1085, #1243,
  #1241, #1265–#1269, and the range #1241–#1278 via
  `gh issue view <n> --repo rjmurillo/moq.analyzers --json state,title`
  (or the GitHub UI if `gh` is unavailable).
- Duplicate closures: #1244/#1246/#1247/#1249/#1252/#1254 are CLOSED as
  duplicates (verified for #1244 on 2026-07-02).
- Dead-branch merge status: `git branch -r --no-merged main | grep -E 'copilot|ci/add-docs|feature/build'`
  (as of 2026-07-02: all listed branches unmerged except `copilot/fix-524`).
- `copilot/fix-496` still contains its self-removal of the proposed
  InSequence analyzer (then ID Moq1203):
  `git log --oneline main..origin/copilot/fix-496 | grep -i 'Remove Moq1203'`
  (and `git show 8037857^:src/Common/DiagnosticIds.cs` shows Moq1203 =
  `InSequenceSetupShouldBeProperlyConfigured` at that point)
- v0.4.1 still contains the CS8032 fix (cherry-picked to the release branch
  as `9c4184f`): `git log v0.4.1 --oneline --grep 'CS8032'` (expect `9c4184f`;
  note `git tag --contains 38943ac` is EMPTY — `38943ac` is the main-branch
  commit, the tag ships its cherry-pick)
- v0.3.0-alpha.1 still contains the packaging fix: `git tag --contains 5f4914b`
- CA1016 suppression still present: `grep -n "CA1016" .editorconfig tests/.editorconfig`
- Audit finding counts (54 = 1C/6H/16M/17L/14I) come from the 2026-07-02
  audit; the filed issues are the durable record if the counts drift.

Last verified: 2026-07-02 against commit 05135b2.
