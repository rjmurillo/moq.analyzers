# Plan Critique: ADR Set Review (ADR-001 through ADR-009)

## Verdict: NEEDS REVISION

Confidence: High

All 9 ADRs are competent and well-structured. The set has no blocking correctness errors. However, 4 issues are P0 because they represent unstated assumptions or missing cross-references that could cause incorrect decisions downstream. 7 issues are P1. 6 are P2.

---

## Strengths

- Each ADR follows a consistent structure (context, decision, consequences, alternatives, implementation notes).
- Rejection rationale for each alternative is specific and data-grounded.
- Performance consequences are quantified in direction if not always in magnitude.
- ADR-006 and ADR-007 together form a coherent semantic analysis strategy.

---

## Issues Found

### P0: Blocking

**P0-1 (ADR-003): Roslyn version floor has no upgrade trigger defined.**

The ADR states 4.8 is pinned because its API surface is "sufficient for all current analyzers." It does not define what event requires revisiting this pin. The phrase "current analyzers" becomes stale the moment a new analyzer is added. There is no stated review criterion, no owner, and no threshold (e.g., "if a new analyzer requires an API absent in 4.8, open an ADR to advance the floor"). Without this, the pin silently becomes an obstacle rather than a deliberate constraint.

Recommendation: Add an explicit trigger condition, such as: "Advance this floor when a required Roslyn API is unavailable in 4.8, or when VS 2022 17.8 drops below 5% of the active install base per telemetry."

**P0-2 (ADR-004): The AnalyzerUtilities 3.3.4 cap has no stated mechanism to revisit.**

The cap exists because 4.x pulls in a newer CodeAnalysis version that conflicts with ADR-003's 4.8 pin. This makes ADR-004 directly dependent on ADR-003, but that dependency is not stated. If ADR-003 is revised to allow a higher Roslyn floor, ADR-004's cap may no longer apply. A reader updating only ADR-003 will not know to check ADR-004.

Recommendation: Add an explicit "Supersession Condition" field to ADR-004: "This cap becomes unnecessary if and when ADR-003 permits a Roslyn version compatible with AnalyzerUtilities 4.x. Revisit at that time."

**P0-3 (ADR-005): The build-time validation target is referenced but not described.**

ADR-005 references "a build-time validation target" that "catches compatibility violations before they reach users." This target is central to the ADR's correctness argument. The ADR does not identify which file contains it, what it checks, or what it does when it fails. If the target is ever deleted or broken, the protection disappears with no visible signal. A reader cannot verify the protection exists without independent code investigation.

Recommendation: Name the MSBuild target and the file it lives in. State what condition triggers failure. A one-sentence description suffices.

**P0-4 (ADR-008): PerfDiff regression thresholds are not documented.**

The ADR states PerfDiff "exits with a non-zero code when regressions exceed thresholds." It does not state what those thresholds are or how they were chosen. A contributor seeing a PerfDiff failure cannot tell whether the threshold is 1%, 5%, or 2 standard deviations. The ADR also does not state whether thresholds differ by metric (mean vs. allocated bytes vs. gen0 collections).

Recommendation: State the threshold values in the ADR. If they live in configuration, cite the file and the specific keys. This is a correctness requirement, not a minor detail.

---

### P1: Important

**P1-1 (ADR-001): Silent suppression behavior is undocumented.**

The ADR notes that if Moq is absent, analyzers "exit early without reporting diagnostics." This is the correct behavior. But the ADR does not address what happens when Moq is present but at a version that has renamed or removed a type. In that scenario, `GetTypeByMetadataName` returns null for the missing type, and the analyzer silently skips checks rather than reporting an error. Users on unsupported Moq versions get no diagnostic output and no explanation. This is a failure mode, not a feature.

Recommendation: Add an explicit statement on supported Moq version range and whether out-of-range versions produce a diagnostic or silent no-op.

**P1-2 (ADR-002): netstandard2.0 vs. netstandard2.1 is not evaluated.**

The alternatives section compares against net8.0 and net9.0 but skips netstandard2.1. netstandard2.1 adds `Span<T>` overloads, `HashCode`, and nullable reference type support in APIs, all of which are relevant to analyzer authoring. The ADR should explain why netstandard2.1 was not considered, or acknowledge that it was implicitly included in "netstandard2.0" reasoning (e.g., because VS/.NET Framework hosts do not support netstandard2.1).

Recommendation: Add a brief note that netstandard2.1 was not considered because NuGet analyzer packages loaded by MSBuild on .NET Framework require netstandard2.0 compatibility.

**P1-3 (ADR-006): Cache invalidation behavior is not described.**

The WellKnownTypes pattern resolves symbols once per `CompilationStartAnalysisContext`. ADR-006 does not state what happens when the compilation changes mid-session (e.g., the user adds or removes a Moq package reference without restarting the IDE). Roslyn should re-invoke the `CompilationStartAnalysisContext` callback for each new compilation, but the ADR does not confirm this or reference it. Developers unfamiliar with Roslyn's incremental model may implement the cache incorrectly.

Recommendation: Add a note confirming that Roslyn issues a new `CompilationStartAnalysisContext` for each incremental compilation, which invalidates and re-creates the `MoqKnownSymbols` instance automatically.

**P1-4 (ADR-007): Mixed analysis strategy has no governing rule.**

ADR-007 states that "some edge cases require examining raw syntax" and still require `RegisterSyntaxNodeAction`. This is acknowledged, but the ADR does not state the decision rule for when to use syntax analysis as a fallback. Without a rule, contributors will make inconsistent choices. Two analyzers handling structurally similar problems may use different registration strategies.

Recommendation: State the rule explicitly: "Use `RegisterOperationAction` by default. Fall back to `RegisterSyntaxNodeAction` only when the construct has no `IOperation` equivalent (e.g., attributes, trivia). Document the reason in a code comment at the registration site."

**P1-5 (ADR-008): Baseline storage strategy has no rotation policy.**

Baseline results are stored in `build/perf/`. The ADR does not state when the baseline is updated, who can update it, or how stale baselines are detected. A baseline that is months old may produce false negatives (regressions that appear to be improvements relative to an outdated baseline) or false positives (improvements flagged as regressions).

Recommendation: State the baseline update policy: "Update the baseline when a deliberate performance improvement is merged. Gate updates on explicit PR approval from a maintainer."

**P1-6 (ADR-009): Test coverage floor is not stated.**

The implementation notes require "at least one positive test and one negative test per scenario." This is a minimum, not a coverage floor. ADR-009 does not state a target code coverage percentage or how coverage is measured. The project MEMORY states 100% test coverage as a standard. The ADR should either reference that standard or state the specific coverage expectation for analyzers.

Recommendation: Add: "Target 100% line coverage for all analyzer and code fix implementations. Coverage is measured in CI."

**P1-7 (ADR-003 / ADR-009 interaction): Test infrastructure Roslyn version is not pinned to match ADR-003.**

ADR-003 pins the compile-time Roslyn reference to 4.8. ADR-009 describes the test infrastructure but does not state whether test-time Roslyn resolves to the same version. If the test project references a newer Roslyn transitively, tests may pass against APIs unavailable at the pinned version. This is a latent compatibility gap.

Recommendation: ADR-009 should explicitly cross-reference ADR-003 and confirm that test Roslyn references resolve to the same major.minor floor.

---

### P2: Minor

**P2-1 (All ADRs): No cross-reference links between dependent ADRs.**

ADR-004 depends on ADR-003. ADR-001 and ADR-006 are closely related (symbol detection and symbol caching are the same concern at different levels). ADR-007 depends on ADR-001. None of the ADRs contain explicit "Related ADRs" or "Depends On" fields. A reader of any single ADR does not know which other decisions constrain it.

Recommendation: Add a "Related Decisions" section to each ADR listing peer ADR numbers and the nature of the relationship (e.g., "ADR-003 constrains this decision; if ADR-003 is revised, revisit").

**P2-2 (ADR-002): The cognitive split between test and analyzer TFMs is noted but not mitigated.**

NEG-003 acknowledges a "cognitive split" between test projects (which can use higher TFMs) and analyzer assemblies (netstandard2.0). The ADR does not suggest any mitigation. Over time, contributors will use higher-TFM APIs in tests and accidentally use them in analyzer code.

Recommendation: Add a note that static analysis (e.g., a build error or Roslyn analyzer) should enforce netstandard2.0 API usage boundaries in analyzer projects.

**P2-3 (ADR-005): Dependabot's behavior with `Directory.Packages.props` is assumed, not verified.**

The ADR states "Dependabot PRs touch `Directory.Packages.props` exclusively." This is the expected behavior with CPM enabled, but it is an assumption about Dependabot configuration. If Dependabot is not configured for CPM mode, it may open PRs against individual `.csproj` files.

Recommendation: Add a note that Dependabot is configured with `versioning-strategy: lockfile-only` or equivalent to enforce CPM-aware updates.

**P2-4 (ADR-008): CI environment differences are not addressed.**

The ADR describes PerfDiff blocking PR merges on regression. Benchmark results vary by CPU, memory pressure, and OS scheduler state. CI environments (GitHub Actions shared runners) are significantly noisier than developer machines. The ADR does not state how the threshold or baseline accounts for CI noise.

Recommendation: Note whether baselines are generated on CI or developer machines, and whether the threshold is calibrated for CI variance.

**P2-5 (ADR-006): `MoqKnownSymbols` growth path is unmanaged.**

NEG-003 notes the class grows as Moq adds API surface. The ADR does not define a cap or refactoring trigger. If Moq adds many new types in a major version, `MoqKnownSymbols` could become a maintenance bottleneck.

Recommendation: Add a note that if the class exceeds a stated size (e.g., 30 type references), refactor into sub-groupings by Moq API area.

**P2-6 (ADR-009): NUnit is not listed as a rejected alternative.**

NUnit is a common .NET test framework. The alternatives section covers MSTest, external fixture files, and manual compilation. The absence of NUnit is unexplained. It may be intentional (NUnit offers no advantage over xUnit for this use case) but should be stated.

Recommendation: Add a one-line NUnit entry: "Not considered. xUnit and NUnit have equivalent capability for this use case; xUnit was the project's existing choice."

---

## Questions for ADR Authors

1. ADR-003: What is the projected VS 2022 17.8 install base decline timeline? What telemetry source governs the upgrade trigger?
2. ADR-004: Is there an active tracking issue for lifting the AnalyzerUtilities cap when ADR-003 is revised?
3. ADR-005: Which MSBuild file contains the compatibility validation target, and what does it emit on failure?
4. ADR-008: What are the exact PerfDiff threshold values, and are they per-metric or aggregate?
5. ADR-001: What Moq version range is explicitly supported? What happens to users on Moq 4.x vs. 5.x?

---

## Approval Conditions

The following must be addressed before this ADR set is considered final:

- P0-1: Add upgrade trigger to ADR-003.
- P0-2: Add supersession condition and cross-reference to ADR-004.
- P0-3: Name the validation target and its file in ADR-005.
- P0-4: Document PerfDiff threshold values in ADR-008.

P1 items should be addressed in follow-up PRs before new analyzers are authored against this ADR set.

---

## Handoff Recommendation

NEEDS REVISION. Route to planner/authors with the P0 list above. P1 items may be addressed in a follow-up pass after P0s are resolved.
