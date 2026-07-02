---
name: moq-analyzers-proof-toolkit
description: "Provides the prove-don't-assume recipes for moq.analyzers — load BEFORE asserting any claim you have not observed: \"Moq has this overload\", \"this snippet triggers/doesn't trigger MoqXXXX\", \"this KnownSymbols entry resolves\", \"the span lands here\", \"Roslyn behaves like X\", \"my change is perf-neutral\", \"the DLL loads in VS\", \"nothing else regressed\". Each recipe is when-to-use / steps / expected output / pitfalls, with a worked example from this repo's history (phantom IReturns symbols, GeneratedReturnsExtensions #1243, ConstructorBody vs MethodBody #1253, void-Setup false negative #1270). Triggers: verifying an API exists, validating proposed test expectations, empirical Roslyn probes, CS8032 checks, pre-merge regression proof. NOT for: the measurement tools' own mechanics and output interpretation (moq-analyzers-diagnostics-and-tooling), corpus/statistics research design (moq-analyzers-research-methodology), or test-suite authoring patterns (moq-analyzers-validation-and-qa)."
---

# Proof toolkit: recipes that catch plausible-but-wrong

This project ships Roslyn analyzers (compiler plugins that inspect code and report
diagnostics) into mission-critical codebases. Its costliest historical failures were
**changes that looked correct but were wrong** — code and tests written from memory of
what Moq or Roslyn "obviously" does. Every recipe below exists because assuming that
thing once produced a real bug. The rule:

> **A claim you have not observed is a hypothesis. Run the probe, paste the output,
> THEN write the code.**

All commands are repo-root relative. In sandboxes: `export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"`.
Verified 2026-07-02 against commit `05135b2`.

## Recipe index

| # | You are about to claim... | Recipe |
|---|---|---|
| 1 | "Moq has/lacks this type, member, or overload" | dotnet-inspect + NuGet-cache reflection |
| 2 | "This code does / does not trigger MoqXXXX" | Run the BUILT DLLs on a snippet |
| 3 | "This KnownSymbols entry resolves in real compilations" | Resolves-non-null test pair |
| 4 | "The diagnostic span lands on this token" | Markup pin + observed span + STOP protocol |
| 5 | "Roslyn API X behaves like Y" | Scratch probe pinned to Roslyn 4.8 |
| 6 | "My change is performance-neutral / faster" | Hypothesis-first BDN + PerfDiff |
| 7 | "The shipped DLL loads in every supported host" | Referenced-assembly check + load-test matrix |
| 8 | "My fix regresses nothing else" | Full-matrix run + span-pin inviolability |

---

## Recipe 1 — Prove a Moq API exists or has an overload

**When to use:** before adding a `MoqKnownSymbols` entry (`src/Common/WellKnown/MoqKnownSymbols.cs`),
before writing analyzer logic that assumes an overload shape, before asserting "Moq
version X supports Y". Moq's fluent surface is large, version-dependent, and full of
near-misses — memory is not evidence.

**Steps:**

1. Primary instrument — `dotnet-inspect` (a CLI that dumps a NuGet package's API
   surface; `dotnet tool install -g dotnet-inspect` if missing). ALWAYS pin the version
   to one of the two the test matrix covers:

   ```bash
   dotnet-inspect member "IReturns<TMock, TResult>" --package Moq@4.18.4 --all
   dotnet-inspect member "IReturns<TMock, TResult>" --package Moq@4.8.2  --all
   ```

   `--all` is required for interface members. Observed 2026-07-02: `Returns` has
   20 overloads in 4.18.4 vs 19 in 4.8.2 (the `Returns(InvocationFunc)` overload is
   the 4.18.4-only addition; the `Returns(Delegate)` catch-all exists in both
   versions) — exactly the kind of gap that matters when a rule must behave on both
   versions.

2. When the question is about an EXACT metadata name (arity, non-generic vs generic,
   nested types), fall back to reflection over the cached package DLL. This probe was
   run verbatim on 2026-07-02:

   ```bash
   mkdir -p /tmp/moq-probe && cd /tmp/moq-probe
   cat > probe.csproj <<'EOF'
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup>
   </Project>
   EOF
   cat > Program.cs <<'EOF'
   using System;
   using System.Reflection;
   Assembly asm = Assembly.LoadFrom(args[0]);
   foreach (string name in args[1..])
       Console.WriteLine($"{name} => {(asm.GetType(name, throwOnError: false) is null ? "NOT FOUND" : "exists")}");
   EOF
   dotnet run --project . -- ~/.nuget/packages/moq/4.18.4/lib/net6.0/Moq.dll \
     'Moq.Language.IReturns' 'Moq.Language.IReturns`1' 'Moq.Language.IReturns`2' 'Moq.GeneratedReturnsExtensions'
   ```

   If the package is not in `~/.nuget/packages/moq/<version>/lib/...`, restore it first
   (any project referencing it, or `dotnet add package`). 4.8.2's newest lib folder is
   `netstandard1.3`; 4.18.4's is `net6.0` (both load fine for metadata queries).

**Expected output (observed 2026-07-02, both 4.8.2 and 4.18.4):**

```text
Moq.Language.IReturns => NOT FOUND
Moq.Language.IReturns`1 => NOT FOUND
Moq.Language.IReturns`2 => exists
Moq.GeneratedReturnsExtensions => exists
```

**Pitfalls:**

| Pitfall | Consequence |
|---|---|
| `dotnet-inspect` fuzzy-matches names: querying `member "IReturns"` or `"IReturns<T>"` silently returns the arity-2 interface (observed 2026-07-02) | You "confirm" that a phantom type exists. dotnet-inspect CANNOT prove absence of an exact metadata name — use the reflection fallback for existence-at-exact-name questions |
| Omitting `@version` queries LATEST Moq on nuget.org, not what this repo tests against | Overload counts and members drift; always pin `@4.8.2` / `@4.18.4` |
| Checking only one Moq version | The test matrix runs both; an API present only in 4.18.4 needs a version-gated test row |

**Worked example (the phantom symbols and issue #1243):** `MoqKnownSymbols` exposes
`IReturns` and `IReturns1` properties (lines 293/298) whose metadata names
`Moq.Language.IReturns` / `Moq.Language.IReturns`1` do not exist in ANY Moq version —
they resolve `null` forever, and every `IsKnownReturnValueMethodName` arm consulting
them is dead weight. Meanwhile the delegate-based `ReturnsAsync` overloads that
Moq1203's string-name fallback was invented to cover actually live in
`Moq.GeneratedReturnsExtensions` — a real class, present in BOTH 4.8.2 and 4.18.4, that
`MoqKnownSymbols` never tracked. Ten minutes with the probes above replaces a
name-based fallback (an ADR-001 violation producing false negatives) with a proper
symbol registration. That is issue #1243 (open, 2026-07-02) end to end: the fallback
existed only because nobody proved where the overloads lived.

---

## Recipe 2 — Prove analyzer behavior empirically

**When to use:** before writing `{|MoqXXXX:...|}` expectations for a new test, before
claiming "this is a false positive/negative", before filing or fixing an issue based on
what an analyzer "should" report. The authority is the BUILT DLL, not the source you
just read.

**Steps:**

1. Write the scenario as a self-contained `.cs` file (own `using Moq;`, own fixture types).
2. Run the live-DLL harness (full mechanics, knobs, and limitations in
   moq-analyzers-diagnostics-and-tooling §1):

   ```bash
   dotnet build src/Analyzers/Moq.Analyzers.csproj   # only if you changed analyzer code
   .claude/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh mysnippet.cs
   ```

3. Compare observed diagnostics (ID + line + column) against your expectation. Only
   then encode the expectation as markup in the test suite.

**Expected output:** one line per diagnostic, `file(line,col): warning MoqXXXX: message`;
`(none)` for a clean snippet; exit 2 + CS errors if the snippet doesn't compile
(distinguish "no diagnostics" from "didn't compile" — a snippet that doesn't compile
proves nothing).

**Worked example (issue #1270's validation discipline, reproduced 2026-07-02):** the
audit's test-coverage issue #1270 states "Every expected diagnostic below was verified
by running the current analyzers against the exact test code" — and that validation is
what caught a live false negative. Probe snippet:

```csharp
var mock = new Mock<Worker>();          // Worker: non-virtual DoVoid() and GetValue()
mock.Setup(x => x.DoVoid());            // binds NON-generic Setup(Expression<Action<T>>)
mock.Setup(x => x.GetValue());          // binds generic Setup<TResult>(...)
```

Observed harness output: Moq1200 fires for `GetValue()` only. The `DoVoid()` setup —
equally broken at runtime — is silent, because `IsMoqSetupMethod`
(`src/Common/ISymbolExtensions.Moq.cs:42`) requires
`symbol is IMethodSymbol { IsGenericMethod: true }`, and void-member setups bind the
non-generic overload. Every setup of a void member is invisible to Setup-family rules
today. Because the expectations were validated empirically, #1270 pins this as
commented known-FN rows instead of shipping rows that assert a diagnostic the analyzer
never produces (which would have "passed" only after someone silently weakened them).

**Pitfalls:** harness severities are forced to `warning` (don't read severity off it);
harness compiles with the host SDK's Roslyn while the test suite pins Roslyn 4.8 —
parse-level differences (C# 13 constructs) can differ; a harness result is evidence,
not a regression test — every FP/FN fix still ships with an issue-linked test.

---

## Recipe 3 — Prove a symbol registration is live

**When to use:** whenever you add or touch a `MoqKnownSymbols` /
`src/Common/WellKnown/` entry. A typo'd metadata name fails SILENTLY: the property
resolves `null`, `CreateLazyMethods` yields an empty array, `IsInstanceOf` returns
false, and the analyzer just... never fires. No error anywhere. (This is how the
phantom `IReturns`/`IReturns1` properties survived — Recipe 1.)

**Steps:** add the four-test pattern from
`tests/Moq.Analyzers.Test/Common/MoqKnownSymbolsTests.ReturnsAndThrows.cs` (the
partial-class family `MoqKnownSymbolsTests.*.cs` is the template):

```csharp
[Fact]
public void GeneratedReturnsExtensions_WithoutMoqReference_ReturnsNull()
{
    MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
    Assert.Null(symbols.GeneratedReturnsExtensions);          // no Moq -> null, no crash
}

[Fact]
public async Task GeneratedReturnsExtensions_WithMoqReference_ReturnsNamedTypeSymbol()
{
    MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
    Assert.NotNull(symbols.GeneratedReturnsExtensions);       // THE resolves-non-null pin
    Assert.Equal("GeneratedReturnsExtensions", symbols.GeneratedReturnsExtensions!.Name);
}
```

plus the same pair for the method group (`Assert.True(...IsEmpty)` without Moq /
`Assert.False(...IsEmpty)` with Moq). Assert `Name` and `Arity` on the resolved type —
`IReturns2_WithMoqReference_ReturnsNamedTypeSymbol` asserts `Arity == 2`, which is what
distinguishes the real interface from its phantom siblings.

**Expected output:** the WithMoq tests FAIL if the metadata name is wrong — that is the
entire point. Run the slice:

```bash
dotnet test --settings ./build/targets/tests/test.runsettings \
  --filter "FullyQualifiedName~MoqKnownSymbolsTests"
```

**Pitfalls:**

- A Without-Moq null test alone proves nothing about liveness — the phantom `IReturns`
  properties pass their Without-Moq tests perfectly (2026-07-02: those are the only
  tests they have; no with-Moq non-null pin exists for them because none can pass).
  The **with-Moq non-null** assertion is the load-bearing one.
- Prove the name with Recipe 1 first, in BOTH Moq versions, or document the version gate.
- `CreateLazyMethods` accepts a null type and yields empty — convenient, but it is
  exactly the mechanism that makes dead registrations silent.

---

## Recipe 4 — Prove a diagnostic span

**When to use:** writing or changing any test markup, or any analyzer change that moves
where a diagnostic is reported. Spans in this project are character-precise and
non-negotiable: the diagnostic must land on the specific token users need highlighted
(the argument list for Moq1002, the variable identifier for Moq1500, the lambda
parameter for Moq1100 — it varies by rule and is pinned by existing tests).

**Steps:**

1. Observe first: run the harness (Recipe 2) and read the `(line,col)` of the real
   diagnostic. Example from the harness's own known-positive sample: Moq1002 reports at
   column 33 — the argument list `(1, true)` — not at the `new` keyword.
2. Encode as markup: `{|Moq1002:(1, true)|}` asserts ID + exact span. The strongest
   form, used when message arguments matter too
   (`tests/Moq.Analyzers.Test/NoMockOfLoggerAnalyzerTests.cs:27`):

   ```csharp
   DiagnosticResult.CompilerWarning("Moq1004").WithSpan("/0/Test1.cs", 8, 29, 8, 36).WithArguments(...)
   ```

3. If a span test fails: read the test failure's expected-vs-actual span locations
   carefully — the framework reports both — and treat the delta as information about
   your syntax-tree navigation, not as a number to patch in the test.

**The STOP protocol (repo law, `.github/copilot-instructions.md:513` and
`CONTRIBUTING.md:1198`):**

> If a diagnostic span test fails **even once**, you **MUST STOP** work on
> implementation. Re-evaluate your entire syntax tree navigation logic. If it fails a
> second time, you must admit failure and request expert human guidance. Do not proceed.

**Pitfalls:** the classic plausible-but-wrong move is adjusting the markup to match
whatever the analyzer currently emits, converting a bug into a pinned expectation.
Direction of proof matters: decide the CORRECT span from the rule's user experience
(and existing sibling pins), then make the analyzer produce it. Never widen a span to a
whole invocation because it's easier to hit. Unmarked code in a test source is a
genuine negative assertion — an unexpected diagnostic anywhere fails the test, so
moving a span silently breaks neighbors (Recipe 8).

---

## Recipe 5 — Prove a Roslyn API behavior when docs are ambiguous

**When to use:** any time analyzer logic depends on a Roslyn behavior you cannot quote
from the API docs with certainty — operation-tree shapes, `SymbolInfo` candidate
semantics, syntax-token layouts, enum member availability in the pinned version. The
shipped analyzers compile against Microsoft.CodeAnalysis **4.8** (ADR-003,
`Directory.Packages.props:21-22`), so the probe must pin 4.8.0 too — behavior and API
availability on your workstation's newer Roslyn prove nothing about the shipped floor.

**Steps:** scratch project OUTSIDE the repo tree (so `Directory.Build.props`/CPM don't
interfere), pinned package, print the fact. This exact probe was run 2026-07-02:

```bash
mkdir -p /tmp/roslyn-probe && cd /tmp/roslyn-probe
cat > probe.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" /></ItemGroup>
</Project>
EOF
cat > Program.cs <<'EOF'
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

const string src = """
class C
{
    public C() { var x = 1; }        // constructor body
    void M()   { var y = 2; }        // method body
}
""";
var tree = CSharpSyntaxTree.ParseText(src);
var compilation = CSharpCompilation.Create("probe", new[] { tree },
    new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
var model = compilation.GetSemanticModel(tree);
foreach (var decl in tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>())
{
    IOperation? root = model.GetOperation(decl);
    while (root?.Parent is not null) root = root.Parent;
    Console.WriteLine($"declarator '{decl.Identifier}': root operation kind = {root?.Kind}");
}
EOF
dotnet run --project .
```

**Expected output (observed 2026-07-02):**

```text
declarator 'x': root operation kind = ConstructorBody
declarator 'y': root operation kind = MethodBody
```

**Worked examples (settled — do not re-probe, reuse the findings):**

- **Issue #1253 (Moq1500):** `GetContainingMember` walked ancestors looking for
  `OperationKind.MethodBody` — but constructor bodies root at
  `OperationKind.ConstructorBody`, so the extremely common xUnit
  constructor-creates-repository pattern was never analyzed. The issue's fix plan rests
  on a probe-built table: method body → `MethodBody`, constructor → `ConstructorBody`,
  property accessor → `MethodBody`, local function → outer `MethodBody`, field
  initializer → no declarator operation at all. One probe, five settled facts, and the
  proof that the `OperationKind.PropertyReference` branch was dead code.
- **Issue #1262 (Moq1100):** `GetRefKind` read only `Modifiers[0]`; the probe facts —
  `(scoped ref int x)` has `Modifiers == [scoped, ref]` while
  `GetDeclaredSymbol(param).RefKind == RefKind.Ref`, and `RefKind.RefReadOnlyParameter`
  EXISTS in Microsoft.CodeAnalysis 4.8.0 — turned "iterate all modifiers" (the naive
  fix) into "delete the syntax derivation, compare symbol RefKinds" (the correct one).
  Probing settled both the bug and the fix shape.

**Pitfalls:** probing against the SDK's bundled Roslyn instead of the 4.8.0 package
(you'll conclude an API exists that the shipped floor lacks); forgetting that the test
suite ALSO compiles test sources with Roslyn 4.8 — default LangVersion is C# 12 there,
so C# 13 syntax (params collections) is unparseable in-suite even though consumers'
newer hosts will feed it to your analyzer (that asymmetry is the root of the open
params-collection crash, issue #1241, 2026-07-02 — whose fix is explicitly
scenario-untestable under the pinned test compiler and relies on a code-comment-
documented conservative guard instead).

---

## Recipe 6 — Prove a performance claim

**When to use:** before writing "this change is perf-neutral" or "this speeds up X" in
a PR. ADR-008 makes PerfDiff's exit code a required merge gate; per-keystroke cost is a
project priority.

**Steps:**

1. **Hypothesis first, numbers second.** Before running anything, write down what you
   expect and why: which benchmark(s) should move, in which direction, roughly how much
   ("removing one `GetSymbolInfo` per invocation should move Moq1100's 1-file case a
   few percent; everything else neutral"). A run you can't predict is a run you can't
   interpret — matching numbers confirm understanding; surprising numbers mean STOP and
   explain before trusting either the change or the measurement. (Full experimental
   discipline: moq-analyzers-research-methodology.)
2. Run the gate-equivalent comparison (BenchmarkDotNet benchmarks +
   PerfDiff differ; mechanics, output paths, and strategy table in
   moq-analyzers-diagnostics-and-tooling §2). Requires PowerShell 7 (`pwsh`) — not
   present in all sandboxes; run on a machine that has it or let CI's `perf` job run it:

   ```bash
   ./build/scripts/perf/CIPerf.sh -filter '*(FileCount: 1)'   # PR fast path, same as CI
   ```

3. Paste the verdict AND the relevant `-report-github.md` table into the PR as evidence.

**Expected output:** PerfDiff exit 0 with per-strategy "No regressions detected"
logging, or exit 1 naming the strategy and benchmark. Thresholds and the per-strategy
defect table: moq-analyzers-diagnostics-and-tooling §2 (canonical); re-derive from
`grep -rn 'Threshold' src/tools/PerfDiff/BDN/Regression/`.

**Pitfalls (all load-bearing, 2026-07-02):** PerfDiff has open correctness defects
(#1265–#1269): benchmarks missing from one side are silently dropped (a PR that breaks
the harness passes green — confirm `perfTest/results` actually contains non-empty
`*full-compressed.json`); the absolute-budget strategies never consult the baseline (a
budget failure may not be YOUR regression — check whether baseline already exceeded
it); infinite ratios are excluded from the verdict. A green perf gate is weaker
evidence than it looks; the hypothesis-first step is what catches what the gate misses.
Noise floor: benchmark reruns jitter — a small "regression" on one run of a change that
touches nothing hot is more likely noise than signal; rerun before believing it.

---

## Recipe 7 — Prove host compatibility

**When to use:** any dependency bump or packaging change touching what ships in
`analyzers/dotnet/cs`. **CS8032** is the compiler warning "An instance of analyzer ...
cannot be created" — the analyzer DLL failed to LOAD in the consumer's compiler
process, so every rule silently vanishes for that consumer. Incident #850 shipped
exactly this (release v0.4.1 existed solely to fix it): the netstandard2.0 analyzer
referenced `System.Collections.Immutable` 9.x, which a .NET 8 host (VS 2022 17.8+)
cannot supply.

**The claim to prove:** every shipped DLL's referenced-assembly versions are ≤ what the
minimum host provides (`System.Collections.Immutable` ≤ 8.0.0.0,
`System.Reflection.Metadata` ≤ 8.0.0.0, `Microsoft.CodeAnalysis` == 4.8.0.0 per ADR-003).

**Steps — local referenced-assembly check** (bash variant verified 2026-07-02; CI's
pwsh equivalent lives inline in `.github/workflows/main.yml` "Validate analyzer host
compatibility"):

```bash
mkdir -p /tmp/refcheck && cd /tmp/refcheck
cat > refcheck.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup>
</Project>
EOF
cat > Program.cs <<'EOF'
using System;
using System.Linq;
using System.Reflection;
foreach (string dll in args)
{
    var asm = Assembly.Load(System.IO.File.ReadAllBytes(dll));
    Console.WriteLine(System.IO.Path.GetFileName(dll));
    foreach (var r in asm.GetReferencedAssemblies()
        .Where(r => r.Name is "System.Collections.Immutable" or "System.Reflection.Metadata" or "Microsoft.CodeAnalysis"))
        Console.WriteLine($"  -> {r.Name} {r.Version}");
}
EOF
REPO=<repo-root>
dotnet run --project . -- \
  $REPO/artifacts/bin/Moq.Analyzers/debug/Moq.Analyzers.dll \
  $REPO/artifacts/bin/Moq.Analyzers/debug/Moq.CodeFixes.dll \
  $REPO/artifacts/bin/Moq.Analyzers/debug/Microsoft.CodeAnalysis.AnalyzerUtilities.dll
```

**Expected output (observed 2026-07-02):** `Microsoft.CodeAnalysis 4.8.0.0` and
`System.Collections.Immutable 8.0.0.0` for the two Moq.* DLLs (AnalyzerUtilities shows
older 3.3/1.x refs — lower is fine; HIGHER than 8.0.0.0 is the failure).

**The triple enforcement (know it before trusting a green build):**

| Layer | Where | What it proves |
|---|---|---|
| `ValidateAnalyzerHostCompatibility` MSBuild target | `build/targets/packaging/Packaging.targets:20` | Resolved package versions ≤ max majors, at build/pack time |
| Inline CI DLL check | `main.yml` "Validate analyzer host compatibility" | The three shipped RELEASE DLLs' actual assembly references |
| `analyzer-load-test` CI job | `main.yml` (9-leg matrix, 2026-07-02: net8/9/10 dotnet-CLI on Linux ARM + net472/48/481 MSBuild on windows-2022 AND windows-2025-vs2026) | The packed nupkg actually loads — build output grepped for CS8032 per leg |

**Pitfalls:** a passing LOCAL build proves nothing — your dev SDK is .NET 10 and
happily provides 9.x/10.x assemblies the VS 2022 host lacks; only the matrix proves
loading. Transitive bumps count: the #850 break came in through a dependency update,
not a direct reference (which is why Renovate ignores Microsoft.CodeAnalysis.* and
AnalyzerUtilities is capped — ADR-004). Never "fix" CS8032 by bumping the Roslyn pin;
that trades old-host compatibility away silently (change-control territory).

---

## Recipe 8 — Prove a fix does not regress siblings

**When to use:** before declaring any analyzer/code-fix change done. The test suite is
a mesh of negative assertions: every UNMARKED line of every test source asserts "no
diagnostic here", `AllAnalyzersVerifier`
(`tests/Moq.Analyzers.Test/Helpers/AllAnalyzersVerifier.cs`) runs every discovered
analyzer over shared no-diagnostics suites, and `TestDataExtensions` fans each row
across 2 namespace styles × up to 2 Moq versions. A change that makes YOUR rule fire in
one more place can fail tests belonging to three other rules — that is the mesh
working.

**Steps:**

1. Full-matrix run, no filter (a `--filter` run proves only the slice you filtered):

   ```bash
   dotnet build /p:PedanticMode=true          # CI-parity: warnings are errors in CI, not locally
   dotnet test --settings ./build/targets/tests/test.runsettings
   ```

   Baseline 2026-07-02: 3,357 tests in Moq.Analyzers.Test + 4 in PerfDiff.Tests; in
   sandboxes whose git remote is not a github.com URL, the 2 `PackageTests.Baseline`
   snapshot tests fail for environment reasons (nuspec repository-URL scrubber) —
   anything ELSE failing is yours.
2. Read every failure through the span-pin lens: **existing span pins are inviolable.**
   If your change moves a pinned span, the default conclusion is that your change is
   wrong, not the pin. Recipe 4's STOP protocol applies to pre-existing tests with full
   force — repointing old markup to make a new feature pass is the single most
   dangerous "fix" in this codebase.
3. For FP/FN fixes: prove the fix red-first — run the new regression test against the
   UNFIXED analyzer and paste the failure (issues #1253/#1262 both demand "confirmed
   failing pre-fix" explicitly). A regression test that never failed proves nothing
   about the bug.
4. If the change touches detection helpers in `src/Common/`, also run the
   Doppelganger suites (`--filter "FullyQualifiedName~Doppelganger"` as a quick slice
   before the full run) — user-defined `Mock<T>` look-alikes must stay silent.

**Expected output:** zero failures beyond the documented sandbox-only PackageTests
pair, plus (for behavior changes) a pasted red-first failure. PR evidence requirements
(what to paste where) are moq-analyzers-change-control's territory.

**Pitfalls:** green-by-filter (the suite you didn't run is the one you broke);
`.received.*` Verify-snapshot files left in the tree after a failure (delete before
commit); "fixing" a sibling's now-failing negative row by marking it — that converts
your regression into their pinned expectation. When a sibling rule's test fails and you
believe the NEW behavior is correct, that is a deliberate behavior change requiring its
own issue and review, not a test edit in passing.

---

## When NOT to use this skill

| If you need... | Load instead |
|---|---|
| The measurement tools themselves — harness knobs, PerfDiff internals, binlogs, coverage, SARIF, snitch, dotnet-inspect subcommand reference | moq-analyzers-diagnostics-and-tooling |
| Corpus-scale FP-rate claims, experiment design, statistics discipline | moq-analyzers-research-methodology |
| Test-suite authoring: markup grammar, ReferenceAssemblyCatalog, MemberData patterns, Verify snapshots | moq-analyzers-validation-and-qa |
| Step-by-step diagnosis strategy for a live bug | moq-analyzers-debugging-playbook |
| Build/SDK/environment failures, PedanticMode, hooks | moq-analyzers-build-and-env |
| Roslyn concepts from zero (symbols, operations, analyzer lifecycle) | roslyn-analyzer-reference |
| Moq semantics themselves (what Setup/Returns/Raise DO) | moq-api-reference |
| ADRs, KnownSymbols architecture, banned APIs | moq-analyzers-architecture-contract |
| Adding/retiring a rule end-to-end | moq-analyzers-rule-lifecycle |
| PR evidence policy, merge gates, release promotion | moq-analyzers-change-control |
| The incidents behind these rules (Moq1203 saga, CS8032, S1135) | moq-analyzers-failure-archaeology |
| Severity/editorconfig configuration of shipped rules | moq-analyzers-config-and-flags |
| Rule docs / writing style | moq-analyzers-docs-and-writing |
| The FP-convergence campaign backlog (#1241–#1278) | moq-analyzers-fp-convergence-campaign |
| Open research questions / beyond-SOTA goals | moq-analyzers-research-frontier |
| BCL/API design standards for public surface | dotnet-api-design-standards |

## Provenance and maintenance

- Phantom symbols still phantom: rerun the Recipe 1 reflection probe against `~/.nuget/packages/moq/4.18.4/lib/net6.0/Moq.dll` — expect the arity-0 and arity-1 `Moq.Language.IReturns` names NOT FOUND, the arity-2 name and `Moq.GeneratedReturnsExtensions` exists. Delete the phantom discussion if #1243's fix removes/repurposes the properties: `grep -n "IReturns =>" src/Common/WellKnown/MoqKnownSymbols.cs`
- Issue states (all open as of 2026-07-02): #1243, #1253, #1262, #1265–#1269, #1270 — check https://github.com/rjmurillo/moq.analyzers/issues/1243 etc.; rewrite worked examples in past tense as they close
- Void-Setup FN still live: rerun the Recipe 2 snippet — Moq1200 on `GetValue()` only; if `DoVoid()` starts reporting, #1270's pinned-FN rows have been superseded. Gate source: `grep -n "IsGenericMethod" src/Common/ISymbolExtensions.Moq.cs` (lines 42, 62 as of 2026-07-02)
- Harness path unchanged: `ls .claude/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh`
- Roslyn pin: `grep -n "Microsoft.CodeAnalysis.CSharp\"" Directory.Packages.props` (4.8 as of 2026-07-02); Recipe 5 probes must track it
- KnownSymbols test template: `ls tests/Moq.Analyzers.Test/Common/MoqKnownSymbolsTests.*.cs`
- STOP protocol wording: `grep -n "even once" .github/copilot-instructions.md CONTRIBUTING.md`
- Host-compat maxima: `grep -n "_MaxSystemCollectionsImmutable\|8.0.0.0" build/targets/packaging/Packaging.targets .github/workflows/main.yml | head`
- Load-test matrix legs: count `tfm:` entries under `analyzer-load-test:` in `.github/workflows/main.yml` (9 as of 2026-07-02)
- PerfDiff thresholds: `grep -rn "Threshold.Parse" src/tools/PerfDiff/BDN/Regression/` (35%; 5%+0.5ms ×2; 250ms; 100ms as of 2026-07-02)
- Test count: `dotnet test --settings ./build/targets/tests/test.runsettings` summary (3,357 + 4 as of 2026-07-02); PackageTests sandbox caveat holds only where the git remote is not a github.com URL
- Moq overload counts (20 vs 19 Returns; the 4.18.4-only addition is `Returns(InvocationFunc)` — `Returns(Delegate)` exists in both): `dotnet-inspect member "IReturns<TMock, TResult>" --package Moq@4.18.4 --all` and `@4.8.2`
- Frontmatter stays parser-safe: `python3 -c "import yaml; print(len(yaml.safe_load(open('.claude/skills/moq-analyzers-proof-toolkit/SKILL.md').read().split('---')[1])['description']))"` — expect the full description length, not an error or a truncated count
- Shipped DLL list (3 files): `grep -n "PackagePath=\"analyzers/dotnet/cs\"" src/Analyzers/Moq.Analyzers.csproj`

Last verified: 2026-07-02 against commit 05135b2.
