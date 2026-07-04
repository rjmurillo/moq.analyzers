---
name: moq-analyzers-fp-convergence-campaign
description: "Execute the decision-gated campaign that makes false-positive/false-negative fixing in moq.analyzers CONVERGE instead of whack-a-mole. Load this when working any correctness-backlog issue (#1241-#1264, #1270), when a new false-positive or false-negative report arrives and you must decide what to fix and in what order, when choosing between fix strategies (register a symbol vs. unwrap syntax vs. guard an operation), when asked \"which correctness issue next\", \"why does Moq1203 keep regressing\", or \"how do we prove this class of bug is dead\". Do NOT load it for diagnosing one bug's root cause (moq-analyzers-debugging-playbook), test-writing mechanics and markup syntax (moq-analyzers-validation-and-qa), the raw chronicle of past incidents (moq-analyzers-failure-archaeology), PR gate rules (moq-analyzers-change-control), or Roslyn API basics (roslyn-analyzer-reference)."
---

# The FP convergence campaign

**Mission (maintainer-designated hardest live problem, 2026-07-02):** make
false-positive fixing converge. The historical failure mode is
*whack-a-mole*: fix one false positive, ship, get a structurally identical
report two weeks later on a slightly different syntax shape. Moq1203 alone
took **five separate patches** over three months (chaining `6ec810c`,
parentheses `c270302`, sibling rules `894313b`, delegate overloads `0bef80b`,
extension methods `5eec7e1`) because each fix addressed one shape instead of
the shape *class*. This skill is the executable plan that ends that.

**Definitions** (used throughout):

- **Analyzer** — a class running inside the C# compiler that reports
  diagnostics (e.g. `Moq1002`) on consumers' code as they type.
- **False positive (FP)** — the analyzer flags correct code. **False
  negative (FN)** — it stays silent on wrong code. FP is worse: it trains
  users to suppress the rule.
- **Escape hatch** — a recurring structural reason a report escaped the
  analyzer's logic (e.g. "the Setup call was wrapped in parentheses").
  Convergence = enumerating the hatches and closing each one **for every
  analyzer**, not just the one reported.
- **Span** — the exact character range a diagnostic underlines; pinned by
  tests, non-negotiable.
- **Gate** — a checkpoint with an exact command and an expected observation.
  Do not proceed past a gate you have not passed.

Every fix routes through normal change control (PR, evidence, CI gates —
see moq-analyzers-change-control). Nothing in this campaign authorizes
going around a gate; the campaign exists to make passing them repeatable.

---

## Campaign map

| Phase | Goal | Gate |
|---|---|---|
| 0 | Inventory the backlog; pin the green baseline | Baseline numbers reproduced |
| 1 | Reproduce the report against the live DLLs, both Moq versions | Diagnostic observed (or FN confirmed silent) |
| 2 | Classify into exactly one escape-hatch class | Class named, historical precedent cited |
| 3 | Pick the ranked fix with its theory obligation | Fix pattern chosen from the settled menu |
| 4 | Prove: issue-linked regression rows, full suite, spans, perf | All commands green |
| 5 | Converge structurally: close the hatch across ALL analyzers | Matrix/corpus/mutation follow-up filed |

---

## Phase 0 — Inventory and baseline

### The correctness backlog (all verified OPEN on GitHub, 2026-07-02)

Every issue below carries a **complete implementation plan in its body**
(exact code, exact test rows, acceptance criteria) — read the issue before
writing any code; do not re-derive a plan that already exists.
(#1244, #1246, #1247, #1249, #1252, #1254 are closed duplicates from a
filing race — if a URL lands you on one, follow it to the open twin.)

**Work strictly in this order.** Crashes disable the analyzer for the whole
IDE session (Roslyn reports AD0001 — "analyzer threw an exception" — and
turns it off); they outrank everything.

| Order | Issue | Rule(s) | Class | One-line defect |
|---|---|---|---|---|
| 1 | #1241 | Moq1001/1002 | Crash | `(IArrayTypeSymbol)` cast throws on C# 13 params collections |
| 2 | #1242 | Moq1100 fixer | Crash | `Arguments[0]` on `mock.Setup().Callback(...)` mid-edit → ArgumentOutOfRangeException |
| 3 | #1250 | (shared) | Crash | `SingleOrDefault` inside cached `Lazy` poisons MockBehavior resolution for the compilation lifetime |
| 4 | #1248 | Moq1202/1204 | FP | Canonical `Raise(m => m.E += null, EventArgs.Empty)` on non-generic `EventHandler` flagged; error-type delegates treated as zero-param |
| 5 | #1251 | Moq1300 | FP | Fires at Error severity on unresolved/type-parameter `As<T>` arguments |
| 6 | #1255 | Moq1400/1410 | FP+FN | Boxed enum constants compared with `==` (reference equality) |
| 7 | #1262 | Moq1100 | FP | `GetRefKind` reads only `Modifiers[0]`; `scoped ref` params flagged |
| 8 | #1256 | Moq1210 fixer | Bad fix | "Make member virtual" emits non-compiling code (static/sealed/struct) |
| 9 | #1257 | Moq1400/1410 fixer | Bad fix | Throws on stale diagnostic shapes; undefined EditType accepted |
| 10 | #1258 | Moq1208 fixer | Bad fix | `ReturnsAsync<T1>` output has no matching generic overload — doesn't compile |
| 11 | #1243 | Moq1203 | FN | Name-based fallback fires even when the symbol resolved to a **non-Moq** method (ADR-001 tension) |
| 12 | #1245 | Moq1001/1002 | FN | Target-typed `new` (`Mock<IFoo> m = new(42);`) never analyzed |
| 13 | #1253 | Moq1500 | FN | Constructor bodies (`OperationKind.ConstructorBody`) never analyzed — the standard xUnit pattern |
| 14 | #1263 | Moq1210 | FN | Named-argument reordering in Verify skips analysis (`Arguments[0]` assumes source order) |
| 15 | #1264 | Moq1200/1207/1210 | FN | Sealed default interface members treated as overridable |
| 16 | #1270 | (many) | Tests | Missing negative/edge coverage for the thin analyzers (the gaps that *hid* #1248 et al.) |

A "Bad fix" is a code fix (the lightbulb suggestion) whose output crashes or
does not compile — treated at FP priority because it actively corrupts user
code.

### GATE 0 — reproduce the green baseline before touching anything

```bash
export PATH="$HOME/.dotnet:$PATH"      # repo builds with .NET SDK 10.0.301 (global.json)
dotnet build /p:PedanticMode=true      # CI-parity: warnings become errors
dotnet test --settings ./build/targets/tests/test.runsettings
```

**Expected (2026-07-02):**

- Build: **0 warnings, 0 errors** (plain `dotnet build` is lenient; the
  `/p:PedanticMode=true` form is what CI runs — always use it).
- Tests: **Moq.Analyzers.Test 3,357 total; PerfDiff.Tests 4/4.** In
  sandboxes whose git remote is not a `https://github.com/...` URL, exactly
  2 `PackageTests.Baseline` snapshot tests fail (the nuspec scrubber expects
  a github.com repository URL) — that is environmental, not a defect.

**If you see X instead → branch to Y:**

| Observed | Branch |
|---|---|
| Build warnings/errors | Stop; environment or a prior change is broken → moq-analyzers-build-and-env |
| MSB4018 `GetBuildVersion` | `git fetch --unshallow` (Nerdbank.GitVersioning needs full history) |
| >2 test failures, or failures outside PackageTests | You are not on a green base — do not start the campaign; bisect first (moq-analyzers-debugging-playbook §7) |
| Span-assert failures | STOP protocol — moq-analyzers-debugging-playbook §1 |

Record the numbers you observed. Every later phase compares against them.

---

## Phase 1 — Reproduce every report

Never fix from the report text. Reports routinely misattribute the rule ID,
omit the Moq version, or paste simplified code that doesn't reproduce.

### 1.1 Build a minimal repro file

Shrink the reported code to one self-contained `.cs` file (own
`using Moq;`, no external types). Discipline: remove one element at a time
and re-run; stop at the smallest file that still shows the behavior. The
*shape* that survives shrinking (a parenthesis, an extension method, a
delegate argument) is usually the escape hatch itself — Phase 2 input.

### 1.2 Run the built analyzer DLLs against it — BOTH Moq versions

The repo ships a live-DLL harness (see moq-analyzers-diagnostics-and-tooling
for its internals). It loads `artifacts/bin/Moq.Analyzers/debug/*.dll` into
a throwaway project with a **real** Moq package reference (analyzers
early-exit via `IsMockReferenced()` when Moq is absent) and raises every
rule to `warning` so Info-severity rules appear:

```bash
# from repo root; build first if needed: dotnet build src/Analyzers/Moq.Analyzers.csproj
.github/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh repro.cs 4.18.4
.github/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh repro.cs 4.8.2
```

4.8.2 and 4.18.4 are the two Moq versions the entire test suite pins
(`tests/Moq.Analyzers.Test/Helpers/ReferenceAssemblyCatalog.cs`). Their API
surfaces differ materially (e.g. `Returns(InvocationFunc)` exists only in
4.18.4; `IProtectedMock` has 25 members vs 14 — see moq-api-reference's
verified version-diff table). A "fix" verified on one version only is half
a fix.

### GATE 1 — the report is reproduced

**Expected:** for an FP report, the harness prints the diagnostic on code
you have confirmed is legal Moq usage (check against moq-api-reference or
the Moq XML docs — *not* intuition). For an FN report, the harness prints
`(none)` on code you have confirmed misbehaves at runtime.

| Observed | Branch |
|---|---|
| Exit 2 (CS errors printed) | Repro doesn't compile. If the report is *about* mid-edit code, that's expected — note it; the fix class is likely "error-type handling" (Phase 2, class 6). Otherwise fix the snippet |
| `(none)` for a claimed FP | Try the other Moq version; try `SNIPPET_TFM`/wrapping variations. Still none → the report may target an older release: `git checkout <tag>` of the reported version, rebuild, re-run. Still none → request info on the issue; do not fix ghosts |
| Diagnostic appears but different rule ID than reported | Re-title your work to the actual rule; reports misattribute IDs often |
| Harness errors "DLL not found" | `dotnet build src/Analyzers/Moq.Analyzers.csproj` |

Attach the exact harness output to the issue. That output is your
before/after evidence in the PR.

---

## Phase 2 — Classify: the escape-hatch decision tree

Every FP/FN this project has ever fixed falls into one of six classes.
Classify before choosing a fix; the class determines the fix pattern AND the
Phase-5 structural obligation. Ask the questions in order; first "yes" wins.

**Q1. Does the repro stop reproducing when you remove a syntactic wrapper
(parentheses, an intermediate fluent call like `.Callback()`, or a
user-defined extension method around `Setup`)?**
→ **Class 1: syntactic-wrapper miss.** The analyzer walked the syntax tree
expecting `mock.Setup(...).Returns(...)` adjacency and the wrapper broke the
walk. Precedents: the whole Moq1203 saga — #849 (chaining), #887
(parentheses), #907 (same bug in Moq1100/1206), #1086 (extension methods).

**Q2. Does the flagged call pass a delegate (method group, lambda-as-
delegate, `Func<>`/`Action<>` argument) where the analyzer expected an
expression, or does it bind a different overload than the analyzer assumed?**
→ **Class 2: delegate-overload resolution miss.** Precedent: #919
(`0bef80b`) — Moq1203/Moq1206 misread delegate-taking `Returns` overloads.

**Q3. Does the analyzer's detection path depend on a Moq type/member that
`MoqKnownSymbols` (`src/Common/WellKnown/MoqKnownSymbols.cs`) does not
register — so symbol comparison silently fails and either a fallback fires
or the rule goes blind?**
→ **Class 3: missing symbol registration.** Precedents: `IRaise<T>` was
unregistered until `35d363d` (#770); `Moq.GeneratedReturnsExtensions`
(delegate-based `ReturnsAsync` overloads) is unregistered **today** — root
cause of #1243.

**Q4. Is the flagged expression a *value* (RHS of a comparison, a static
member access, a constant) that the analyzer treated as a member-access
pattern to validate?**
→ **Class 4: value-expression guard miss.** Precedent: Moq1302
LINQ-to-Mocks FPs #1010, fixed `4b705e2` (#1017) + regression suite
`3399297` (#1020). Settled discipline: register the `OperationKind` → guard
`operation.Instance is null` (static = value expression, skip) → only then
validate the member.

**Q5. Does the behavior differ between Moq 4.8.2 and 4.18.4 in the harness?**
→ **Class 5: version-surface diff.** The analyzer assumed an overload/type
that exists in only one pinned version. Fix must be correct on both; test
rows may split via `WithOldMoqReferenceAssemblyGroups()` /
`WithNewMoqReferenceAssemblyGroups()` when behavior legitimately differs.

**Q6. Does the repro involve code that does not compile (unresolved type,
missing argument, mid-keystroke state)?**
→ **Class 6: error-type / mid-edit handling.** The analyzer treated
`TypeKind.Error` or an empty argument list as analyzable. Precedents: #1248
(defect 2 — error-type delegate treated as zero-parameter), #1251 (error
type reaches the report), #1242 (unguarded `Arguments[0]` crash).

### GATE 2 — classification recorded

**Expected:** one class, one historical precedent, written into the issue as
a comment (or into your working notes). If the repro genuinely fits **no
class**, you may have found a seventh escape hatch: document the candidate
class explicitly in the issue and flag it for the maintainer — extending
this tree is a campaign event, not a footnote.

---

## Phase 3 — The fix menu (ranked, each with its theory obligation)

Pick the pattern matching your Phase-2 class. Each entry names the settled
code to copy and the **theory obligation** — the test you must write that
proves the *mechanism*, not just the reported symptom.

### (a) Register the missing symbol — for Class 3

Pattern to copy: property style in `src/Common/WellKnown/KnownSymbols.cs` /
`MoqKnownSymbols.cs` —
`TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IRaiseable")` for
types, `CreateLazyMethods(Type, "Name")` for member groups. Symbols are
created once per compilation (ADR-006) and are the ONLY sanctioned way to
identify Moq API (ADR-001; `Compilation.GetTypeByMetadataName` is banned by
`BannedSymbols.txt`).

**Theory obligation — the resolves-non-null test.** `MoqKnownSymbols` has
**phantom properties** (`IReturns`, `IReturns1`) for types that do not exist
in any Moq version; they resolve null forever, and
`tests/Moq.Analyzers.Test/Common/MoqKnownSymbolsTests.ReturnsAndThrows.cs`
pins that. So for every symbol you register, add BOTH:

1. a with-Moq test asserting the property is **non-null** (else you may
   have registered a phantom and your analyzer silently never fires — an FN
   you shipped while "fixing" an FP), and
2. a without-Moq test asserting **null**/empty (the existing pattern in the
   `MoqKnownSymbolsTests.*.cs` partials).

Verify the metadata name against BOTH Moq packages before registering
(`dotnet tool install -g dotnet-inspect; dotnet-inspect member "<Type>"
--package Moq --all`, or decompile the packages the tests restore).

### (b) Unwrap the syntactic wrapper — for Classes 1 and 2

Do NOT write new tree-walking code. The settled helpers already exist:

| Helper | Location | What it handles |
|---|---|---|
| `WalkDownParentheses()` | `src/Common/SyntaxNodeExtensions.cs:65` | `((mock.Setup(...)))` — any depth of parens |
| `WalkUpParentheses()` | `src/Common/SyntaxNodeExtensions.cs` (same file) | the inverse direction |
| `FindSetupInvocation()` | `src/Common/InvocationExpressionSyntaxExtensions.cs:27` | walks the fluent chain up past `.Callback()` etc., parens included, depth-capped at 10, symbol-checks each candidate via `IsMoqSetupMethod` |
| `IsMoqFluentInvocation` | `src/Common/SemanticModelExtensions.cs:254` | the sanctioned name-fast-path-then-symbol-check shape |

**Theory obligation — the wrapper axis.** Regression rows for the reported
shape are not enough; add rows for ALL FOUR wrapper shapes (plain,
parenthesized, chained-through-`Callback`, wrapped-in-extension-method) even
though only one was reported. That is the difference between patch #2 and
patch #5 of the Moq1203 saga. Copy the issue-linked `MemberData` naming from
`tests/Moq.Analyzers.Test/MethodSetupShouldSpecifyReturnValueAnalyzerTests.cs`
(`Issue849_FalsePositiveTestData`, `Issue887_ParenthesizedSetupTestData`,
`Issue1067_WrappedSetupTestData` — with the issue URL in a comment above).

### (c) Guard the operation with precision — for Classes 4 and 6

The Moq1302 three-phase walker discipline (from #1017):

1. register the narrowest `OperationKind`/`SyntaxKind`;
2. **guard before interpreting** — `operation.Instance is null` means a
   static/value expression, skip; `TypeKind.Error` means mid-edit, skip;
   `Arguments.Count == 0` means mid-edit, skip;
3. only then validate the member and report.

**Conservative-bail direction (memorize this):** when the analyzer *cannot
verify*, it must go **silent** (accept), never speculate. #1241's plan
documents the trap: a naive `return false` bail on an uncastable params type
converts a crash into a *false positive* — the correct bail is `return true`
("assume it matches"). A rare FN on exotic code is acceptable; a crash or an
FP on legal code is not. When touching `Try*` helpers, failure must
propagate as `false`, never as a plausible-looking empty result (#1248
defect 2: `[]` was indistinguishable from "zero-parameter delegate").

**Theory obligation:** negative rows for the value-expression / static /
const / literal variants (the adversarial-boundary set from the #511
doctrine: AI-written code and AI-written tests share blind spots — add the
boundary cases a hostile reviewer would), plus mid-edit rows using
`CompilerDiagnostics.None` (see moq-analyzers-validation-and-qa for the
markup mechanics). Mid-edit coverage is the weakest systematic gap in the
suite today (2 of 10 sampled test classes have any — 2026-07-02 audit).

### Ranking when multiple patterns apply

Symbol registration (a) > syntax unwrapping (b) > guards (c): fix at the
most semantic layer available. If a name-based check would "work", it is
wrong here — see Fenced wrong paths.

---

## Phase 4 — Prove it

Every FP/FN fix ships in the same PR with issue-linked regression tests
across the **mandatory axes**:

| Axis | Mechanism | Non-negotiable? |
|---|---|---|
| 2 namespaces × 2 Moq versions | end every data set with `.WithNamespaces().WithMoqReferenceAssemblyGroups()` (`tests/.../Helpers/TestDataExtensions.cs`) | Yes (split versions only for genuine Class-5 surface diffs) |
| Exact span | `{\|Moq1202:...\|}` markup asserts ID + character-precise span | Yes |
| Issue linkage | `Issue<N>_<Behavior>TestData` member name + issue URL comment | Yes |
| Wrapper shapes | the four shapes from 3(b), for Class 1/2 fixes | Yes for those classes |
| Negative rows | unmarked legal code = genuine "no diagnostic" assertion | Yes |
| Doppelganger | user-defined `Mock<T>` look-alikes must NOT trigger (`DoppelgangerTestHelper`) | When detection logic changed |
| Mid-edit | `CompilerDiagnostics.None` rows | Yes for Class 6 |

Test code must be valid, compiling C# (except deliberate
`CompilerDiagnostics.None` rows) — never write a setup for a static/const
member that wouldn't compile; the verifier treats compile errors as test
failures.

### GATE 4 — the full proof

```bash
dotnet format --verify-no-changes
dotnet build /p:PedanticMode=true                                  # expect 0 warnings
dotnet test --settings ./build/targets/tests/test.runsettings      # expect baseline count + your new rows
# re-run the Phase-1 harness on the repro, BOTH versions — expect flipped outcome
.github/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh repro.cs 4.18.4
.github/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh repro.cs 4.8.2
# perf gate (ADR-008; required PR check). Local pre-flight:
pwsh build/scripts/perf/PerfCore.ps1 -diff -ci -filter "'*(FileCount: 1)'"
```

| Observed | Branch |
|---|---|
| Any pre-existing span pin fails | STOP after 1 failure, escalate after 2 (moq-analyzers-debugging-playbook §1). Never "fix" the pin to match your output |
| Test count went DOWN | An analyzer likely fell out of `AllAnalyzersVerifier` discovery (namespace must be exactly `Moq.Analyzers`) — playbook §9 |
| Perf regression reported | Playbook §10 decides real-vs-tool; note PerfDiff itself has known verdict bugs (#1265–#1269) — do not "tune" thresholds to pass |
| Harness still shows the FP on 4.8.2 only | You fixed one version; back to Phase 2, likely Class 5 |

PR body carries the evidence block required by change control (format/
build/test output, coverage, Moq-version compatibility note). If the fix
touched shared `src/Common` helpers, list every rule that consumes the
helper and state why each is unaffected (or add rows for it).

---

## Phase 5 — Converge structurally (what makes this a campaign)

A merged fix closes one report. Convergence means the *class* can't recur.
After every Phase-4 merge, do the matching structural step:

### 5.1 The wrapper-axis matrix (apply the hatch to ALL chain analyzers)

Every analyzer that reasons about `Setup`/`Returns`/`Raises` fluent chains
is exposed to Classes 1 and 2, not just the one that got reported. The
chain analyzers (from `src/Common/DiagnosticIds.cs`):

Moq1100, Moq1101, Moq1200, Moq1201, Moq1202, Moq1203, Moq1204, Moq1205,
Moq1206, Moq1207, Moq1208, Moq1210.

Status (2026-07-02): only Moq1203 is near-exhaustive on the wrapper axis
(five sagas' worth of regression suites); Moq1100/Moq1206 got partial
coverage in #907/#919; the rest are **unproven** on parenthesized /
extension-method / chained shapes. When you close a Class-1/2 issue for one
rule, add (or file under #1270) the same four wrapper rows for every chain
analyzer above whose test class lacks them. Grep check:

```bash
grep -L "Parenthesized\|WrappedSetup\|extension" tests/Moq.Analyzers.Test/*AnalyzerTests.cs
```

### 5.2 Corpus validation — **open/candidate (not yet built), 2026-07-02**

Goal (maintainer's stated beyond-state-of-the-art target): provably-zero-FP
runs of the built analyzers across real-world OSS codebases that use Moq.
Nothing is implemented; the first three steps are all achievable with
in-repo pieces:

1. `dotnet build` — produces both the DLLs
   (`artifacts/bin/Moq.Analyzers/debug/`) and the package
   (`artifacts/package/debug/Moq.Analyzers.<version>.nupkg`).
2. Inject into a checked-out OSS test project exactly the way the snippet
   harness does — three `<Analyzer Include="...">` items plus a generated
   `.editorconfig` raising all `Moq*` IDs to `warning` (see the csproj
   template inside `run-analyzer-on-snippet.sh`); or add the nupkg via a
   local NuGet source.
3. Build the corpus project, grep the binlog/console for `Moq[0-9]{4}`, and
   adjudicate every hit as TP or FP (each FP becomes a Phase-1 repro).
Unsolved parts (why this stays "candidate"): corpus selection, runtime
budget, and FP adjudication at scale. If you build this, propose it as an
issue first — do not wire new CI jobs unilaterally (change control).

### 5.3 Mutation testing — the FN counterpart — **open (#904, verified OPEN 2026-07-02)**

FPs surface via user reports; FNs don't — silent misses have no reporter.
Issue #904 (adopt Stryker.NET) is the planned systematic FN detector: it
mutates analyzer source and checks whether any test notices. Until it
lands, the FN discipline is manual: every FP fix must ask "what is the
symmetric FN?" (e.g. #1248 fixed an FP *and* tightened the
`EventHandler<T>`-with-non-EventArgs FN in the same change; #1255 is an FP
and an FN from one defect). Known pinned FN: the non-generic
`Setup(Expression<Action<T>>)` overload (void members) is never analyzed —
commented rows in #1270 pin it.

### 5.4 Burn-down bookkeeping

After each merge: check the issue's acceptance boxes, close via
`Fixes #<n>` in the squash commit, and verify the next issue in the Phase-0
table is still the right next target (crash > FP > bad-fix > FN ordering).

---

## Fenced wrong paths (do not re-fight settled battles)

Each of these was tried, failed, and is closed. Re-proposing one without
new evidence wastes a review cycle (full chronicle:
moq-analyzers-failure-archaeology).

| Tempting shortcut | Why it is fenced | Proof |
|---|---|---|
| String-name fallback ("just check the method is named `Returns`") | ADR-001 forbids name-based *semantic* decisions; names are user-forgeable (doppelgangers). A premature fallback *removal* also failed — removal requires symbol-coverage proof first | Failed removal documented in `5172cf3` (#768); correct fix `35d363d` (#770); #1243 is an ungated fallback biting today |
| Delete/abandon the rule instead of fixing it | Rejected. The shipped Moq1203 FP saga was resolved by five targeted fixes, never by deletion. The one recorded rule deletion (branch `copilot/fix-496`, `8037857`) removed the branch's OWN newly proposed InSequence analyzer — which then held the unused Moq1203 ID — after the maintainer's refute-by-construction on PR #504 ("no real value for this analyzer") | PR #504; `git log origin/copilot/fix-496`; moq-analyzers-failure-archaeology entry 2 |
| Widen a diagnostic span so a failing pin passes | Spans are character-precise contract; a span test failure means YOUR change is wrong or needs escalation, never the pin | Playbook §1 STOP protocol |
| Suppress/downgrade the rule severity instead of fixing | Ships the defect to every consumer and hides the signal. (Exception that proves the rule: Codacy CA1016 was a *tooling* FP, suppressed by maintainer decision `58924f7`) | Priority list: crash > FP/FN > perf |
| Skip the second Moq version ("4.18.4 passed") | Half the user base and half the API surface; version diffs are escape-hatch Class 5 | `ReferenceAssemblyCatalog.cs` pins both |
| Route around change control (edit `AnalyzerReleases.Shipped.md`, skip perf gate, merge without evidence) | The gates are the product's crash-history scar tissue | moq-analyzers-change-control |

---

## Success is measured, not felt

| Metric | Definition | How to read it |
|---|---|---|
| Backlog burn-down | Open count of the Phase-0 table | `gh issue list -R rjmurillo/moq.analyzers --label correctness --state open` (issues #1241+ carry the `correctness` label) |
| FP-report reopen rate | A closed FP issue reopened, or a new FP filed against the same rule × same escape-hatch class | Search closed FP issues per rule; two reports in the same cell = the Phase-5 step was skipped |
| Hatch-class extinction | Escape-hatch classes with zero new reports since their matrix sweep (5.1) | Class 1 for Moq1203: no new wrapper FP since `5eec7e1` (2026-05-07) — the existence proof that the method converges |
| Suite growth with green baseline | Test count strictly grows; 0-warning build holds | Gate 0 numbers vs. last recorded |

---

## When NOT to use this skill

- Diagnosing why one analyzer misbehaves, span-pin STOP protocol, AD0001 /
  CS8032 triage → **moq-analyzers-debugging-playbook** (its FP-triage
  section hands off to this skill for classification and burn-down).
- Test markup syntax, verifier helpers, coverage evidence →
  **moq-analyzers-validation-and-qa**.
- The full incident chronicle behind the fenced paths →
  **moq-analyzers-failure-archaeology**.
- PR gates, release promotion, what may be edited →
  **moq-analyzers-change-control**.
- Building/running the repo, SDK setup → **moq-analyzers-build-and-env**.
- Roslyn concepts (symbols vs. syntax, OperationKind) →
  **roslyn-analyzer-reference**; Moq API ground truth →
  **moq-api-reference**.
- Snippet-harness internals and other inspection tooling →
  **moq-analyzers-diagnostics-and-tooling**.
- Authoring a brand-new rule end-to-end → **moq-analyzers-rule-lifecycle**.
- Severity/editorconfig configuration questions →
  **moq-analyzers-config-and-flags**; architecture invariants (ADR-001/006/
  007 details) → **moq-analyzers-architecture-contract**; API-shape review
  → **dotnet-api-design-standards**; docs writing →
  **moq-analyzers-docs-and-writing**; unproven research directions →
  **moq-analyzers-research-frontier** / **moq-analyzers-research-methodology**;
  proof tooling → **moq-analyzers-proof-toolkit**.

---

## Provenance and maintenance

Re-verify anything volatile before relying on it:

- Backlog states: `gh issue list -R rjmurillo/moq.analyzers --state open --search "1241..1270 in:number"` (or open each of #1241–#1264, #1270; #1244/#1246/#1247/#1249/#1252/#1254 are closed duplicates).
- Baseline test count (3,357 + 4) and 0-warning build: run the GATE 0 commands and record fresh numbers.
- Moq version pins (4.8.2 / 4.18.4): `grep -n "PackageIdentity" tests/Moq.Analyzers.Test/Helpers/ReferenceAssemblyCatalog.cs`.
- Saga commits still resolve: `for c in 6ec810c c270302 894313b 0bef80b 5eec7e1 4b705e2 3399297 5172cf3 35d363d; do git log -1 --oneline $c; done`.
- Helper line anchors: `grep -n "WalkDownParentheses" src/Common/SyntaxNodeExtensions.cs; grep -n "FindSetupInvocation" src/Common/InvocationExpressionSyntaxExtensions.cs`.
- Phantom-symbol pins: `grep -n "IReturns" tests/Moq.Analyzers.Test/Common/MoqKnownSymbolsTests.ReturnsAndThrows.cs`.
- copilot/fix-496 self-removal of its proposed InSequence analyzer (then ID Moq1203): `git log --oneline origin/copilot/fix-496 | head -3`.
- Mutation-testing status: `gh issue view 904 -R rjmurillo/moq.analyzers`.
- Corpus validation remains unimplemented: search issues for "corpus"; if an issue/CI job now exists, update §5.2 from candidate to live.
- Harness script still present: `ls .github/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh`.
- Chain-analyzer ID list: `grep -n "const string" src/Common/DiagnosticIds.cs`.

Last verified: 2026-07-02 against commit 05135b2.
