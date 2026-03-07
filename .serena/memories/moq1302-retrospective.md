# Retrospective: Moq1302 False Positive Escaped to Customers (Issue #1010)

**Date**: 2026-03-06
**Analyzer**: `LinqToMocksExpressionShouldBeValidAnalyzer` (Moq1302)
**Rule**: LINQ to Mocks expression should use only mockable members
**Outcome**: False positive shipped in v0.4.0. Escaped code review, CI, and test suite.

---

## Phase 0: Data Gathering

### 4-Step Debrief

#### Step 1: Observe

- Analyzer introduced: commit `458ca5d` (PR #511), authored by Copilot, Jun 25 2025
- PR title: "Lacking coverage for LINQ to Mocks"
- Test file: `LinqToMocksExpressionShouldBeValidAnalyzerTests.cs`, also in `458ca5d`
- One test refactor: commit `1d5a6d0` (PR #635, "improve code coverage")
- `ShouldNotReportFalsePositiveForStaticExternalProperty` test -- covering `StatusCodes.Status200OK` on RHS -- was present at time of investigation (current state). Its commit origin is `1d5a6d0`, NOT the original `458ca5d`.
- The `IBinaryOperation` case in `AnalyzeLambdaBody` unconditionally recurses into BOTH operands via `AnalyzeMemberOperations`
- `AnalyzeMemberOperations` ultimately calls `AnalyzeLambdaBody`, which handles `IFieldReferenceOperation` by calling `AnalyzeMemberSymbol`
- `AnalyzeMemberSymbol` only skips interface members and compiler-error spans; it flags non-virtual fields on concrete types
- `StatusCodes.Status200OK` is a `const int` field on a concrete static class -- non-virtual, non-interface -- so the analyzer reports it

#### Step 2: Respond

- Original PR had no test covering `r.Status == StatusCodes.Status200OK` (RHS = static/const from concrete class)
- `ValidExpressionTestData` covered: interface properties, virtual properties, abstract properties, methods -- all on the LEFT of `==`
- No test exercised a binary comparison where the RHS is a constant or static member from an unrelated concrete class
- `EdgeCaseExpressionTestData` covered `r => 42 == 42` (literal) but not `r => r.X == SomeClass.Constant`
- The false positive only surfaces when real-world code crosses the mock lambda boundary with an external value reference

#### Step 3: Analyze

- The design assumption embedded in the code: "both operands of a binary expression in a Mock.Of lambda are member accesses on the mocked type." This assumption is wrong.
- The RHS of a comparison in `Mock.Of<T>(x => x.Prop == someValue)` can be ANY expression: literal, local, static member, method call, etc.
- The correct model: only the LEFT side (or more precisely, the side referencing the lambda parameter) needs mockability analysis. The RHS is a value expression, not a member setup target.
- The code comment says "Handle binary expressions like equality comparisons" -- but it treats both sides identically, which is the defect.

#### Step 4: Apply

- Fix: In `IBinaryOperation` case, only recurse into the operand that references the lambda parameter. The other operand is a value expression and must be skipped.
- Tests must cover RHS = static property, RHS = constant field, RHS = local variable, RHS = method call on unrelated type.

### Execution Trace

| Commit | Author | What |
|--------|--------|------|
| `458ca5d` | Copilot | Introduced analyzer + initial tests (no RHS static coverage) |
| `3b16ea4` | human | Updated metadata only |
| `1d5a6d0` | (refactor) | Added `StatusCodes` test (the missing boundary case) |
| Issue #1010 | customer | Reported false positive in the wild |

### Outcome Classification

- **Glad**: Moq1302 shipped and covered the primary use cases (interface members, virtual, abstract)
- **Sad**: Static/const RHS false positive introduced with the original feature -- not caught until customer report
- **Mad**: Issue #1010 reached customers; required a patch cycle

---

## Phase 1: Insights

### Five Whys

**Problem**: Moq1302 false-positive when RHS of comparison is a static property/constant from a concrete class.

**Q1**: Why did the analyzer flag `StatusCodes.Status200OK`?
**A1**: `AnalyzeLambdaBody` recurses into both operands of `IBinaryOperation` via `AnalyzeMemberOperations`.

**Q2**: Why does it recurse into both operands?
**A2**: The implementation assumes both operands could be member accesses on the mocked type that need validation.

**Q3**: Why was that assumption made?
**A3**: The Copilot-generated code generalized the binary case symmetrically without modeling the semantic roles of each operand (setup target vs. comparison value).

**Q4**: Why wasn't the wrong assumption caught in review?
**A4**: The test suite only exercised expressions where the RHS was a literal (`true`, `"test"`, `42`) -- not a member reference on an unrelated concrete type. The test set was author-generated alongside the implementation, so the bias in implementation carried into the bias in tests.

**Q5**: Why did the test set share the same bias as the implementation?
**A5**: Copilot generated both together in a single PR. No independent adversarial test authorship. No boundary-case checklist requiring "external type references on RHS" coverage.

**Root Cause**: Co-authored implementation and tests (same agent, same PR) cannot catch shared blind spots. The test author knew what the implementation "expected" and wrote tests to pass it, not to break it.

**Actionable Fix**: Binary operation analysis must distinguish operand semantic roles. Tests must be authored with an explicit checklist of boundary cases that includes: RHS = literal, RHS = local variable, RHS = static member of unrelated type, RHS = constant, RHS = method call on unrelated type.

### Pattern: Recurring Themes

This is NOT an isolated defect. The git log shows:

- PR #4cf69db: "Remove false positive for Moq1200 when using parameterized lambda"
- PR #9c05b6b: "Moq1203 false positive when Setup call is wrapped in parentheses"
- PR #ca1949a: "Moq1203 false positives for ReturnsAsync and Callback chaining"
- PR #6621540: "Resolve delegate-overload resolution for Moq1203 and Moq1206"
- PR #8a82731: "Use semantic model to resolve implicitly typed lambda parameters"

**Pattern**: False positives recur when analyzers process expression sub-trees without distinguishing the semantic role of each node. Operators (binary, ternary, coalesce) have operands with different purposes. Treating them symmetrically causes false positives on the "value" operand.

### Fishbone Analysis

**Problem**: Moq1302 false positive on `x.Prop == ExternalClass.Constant`

| Category | Contributing Factor |
|----------|-------------------|
| Prompt | PR #511 framing: "lacking coverage" -- emphasis on adding cases, not on correctness of existing logic |
| Tools | Copilot as both implementer and test author in a single PR |
| Context | No existing checklist of RHS boundary cases for binary expressions |
| Sequence | Tests authored alongside implementation (no adversarial separation) |
| State | Moq1302 marked as unshipped at time of introduction; later shipped in v0.4.0 without additional test expansion |
| Design | `AnalyzeLambdaBody` uses symmetric recursion over binary operands; no operand-role modeling |

---

## Phase 2: Diagnosis

### Atomicity Score of Root Cause

Score: 35% systemic, 65% isolated

- The specific bug (symmetric binary recursion) is isolated and fixable in ~5 lines.
- The underlying cause (co-authored implementation + tests, no adversarial boundary checklist) is systemic -- same pattern produced Moq1200, Moq1203 false positives.
- Systemic score is 35% because the project has a known pattern of this failure mode across multiple analyzers.

### Test Coverage Gap Analysis

Original test data covered:

- RHS = literal (`true`, `"test"`, `3`, `42`)
- Lambda expressions on interface, virtual, abstract members
- `null` comparisons
- Literal-to-literal comparisons (`42 == 42`)

Test data missing at ship time:

- RHS = static property on concrete class (e.g., `StatusCodes.Status202Accepted`)
- RHS = const field on concrete class (e.g., `StatusCodes.Status200OK`)
- RHS = local variable
- RHS = method call on unrelated type
- RHS = nested property chain on unrelated type

**Gap size**: 5 missing boundary categories. Only 1 was added (eventually) in commit `1d5a6d0`, likely after issue #1010 was filed or as part of the investigation.

---

## Phase 3: Decisions

### Action Classification

| Finding | Operation | Skill ID |
|---------|-----------|----------|
| Symmetric binary recursion is wrong design | ADD | skill-binary-op-operand-roles |
| Co-authored impl+tests share blind spots | ADD | skill-adversarial-test-authorship |
| RHS boundary checklist missing | ADD | skill-rhs-boundary-test-checklist |
| Moq1302 false positive pattern matches prior Moq1203 pattern | TAG | existing false-positive pattern |

### SMART Validation: Key Skills

Skill 1: "Analyze only the lambda-parameter-referencing operand in binary expressions; skip value operands."

- Specific: Yes (one rule)
- Measurable: Yes (no false positives on RHS static members)
- Attainable: Yes
- Relevant: Yes (applies to all binary-expression analyzers)
- Timely: Yes (clear trigger: IBinaryOperation in lambda analysis)
- Score: 90%

Skill 2: "When Copilot generates both implementation and tests in one PR, require independent adversarial test cases from a human reviewer."

- Specific: Yes
- Measurable: Yes (PR review checklist item)
- Attainable: Yes
- Relevant: Yes (process control)
- Timely: Yes (applies at PR review)
- Score: 85%

---

## Phase 4: Extracted Learnings

### Learning 1

- **Statement**: In binary expressions within lambda analysis, only recurse into the operand referencing the lambda parameter.
- **Atomicity Score**: 90%
- **Evidence**: Issue #1010; `AnalyzeLambdaBody` IBinaryOperation case; `StatusCodes.Status200OK` false positive
- **Operation**: ADD skill

### Learning 2

- **Statement**: Test authors who also wrote the implementation share its blind spots; require independent adversarial boundary cases.
- **Atomicity Score**: 85%
- **Evidence**: PR #511 Copilot-generated impl+tests; RHS static member cases absent until PR #635
- **Operation**: ADD skill

### Learning 3

- **Statement**: For any binary/comparison operation in an analyzer, test all RHS types: literal, local, static member, const, method call, external type chain.
- **Atomicity Score**: 80%
- **Evidence**: 5 missing RHS boundary categories allowed #1010 to escape
- **Operation**: ADD skill (boundary checklist)

### Learning 4

- **Statement**: Moq analyzer false positives cluster around expression-tree nodes with multiple semantic roles (binary ops, method chains, parenthesized expressions).
- **Atomicity Score**: 75%
- **Evidence**: Moq1200 PR #301, Moq1203 PRs #895/#886/#919, Moq1302 #1010
- **Operation**: ADD pattern observation

---

## Phase 5: Recursive Learning Extraction Summary

- **Iterations**: 1
- **Skills identified**: 4
- **Atomicity threshold met (>=70%)**: 4/4
- **Duplicates**: 0 (no prior skills on binary-operand-role analysis found)

---

## Phase 6: Close

### +/Delta

**+**: Evidence trail was complete. Git log, analyzer source, and test file provided clear causal chain.

**Delta**: Moq1302 had no separate test data directory (unlike some other analyzers). All test cases are inline in one file. Harder to see coverage gaps at a glance. Consider moving complex expression data to separate `.cs` test data files for visibility.

### ROTI: 3 (High return)

Clear root cause, systemic pattern identified, 4 atomic learnings extracted, directly actionable fix identified.

### Helped, Hindered, Hypothesis

**Helped**: Git log showed exact commit provenance. Copilot authorship of both impl and tests was immediately visible.

**Hindered**: No separate test data directory; all coverage visible only by reading the test class carefully.

**Hypothesis**: Adding a PR checklist item "if AI generated both impl and tests, reviewer must add at least 3 adversarial boundary cases" would reduce this class of escape.

---

## Retrospective Handoff

### Skill Candidates

| Skill ID | Statement | Atomicity | Operation |
|----------|-----------|-----------|-----------|
| binary-op-operand-roles | Analyze only the lambda-parameter operand in binary expressions; skip value operands. | 90% | ADD |
| adversarial-test-authorship | Require independent adversarial boundary cases when AI generates both implementation and tests. | 85% | ADD |
| rhs-boundary-test-checklist | Test all RHS types (literal, local, static member, const, method call) for binary comparison analyzers. | 80% | ADD |
| expression-tree-false-positive-pattern | Moq analyzer false positives cluster around binary/multi-role expression-tree nodes. | 75% | ADD |

### Recommended Next

1. Fix `AnalyzeLambdaBody`: in the `IBinaryOperation` case, detect which operand references the lambda parameter before recursing.
2. Add PR review checklist item for AI-co-authored PRs.
3. Add test cases covering all 5 missing RHS boundary categories for Moq1302.
