# Skill Sidecar Learnings: Analyzer Testing & Quality

**Last Updated**: 2026-03-06
**Sessions Analyzed**: 1

## Constraints (HIGH confidence)

- For binary expressions in lambda analysis, test ALL operand types: literals, static members, const fields, enum values, method calls on unrelated types, local variables, instance properties on external objects. Literal-only tests miss systematic false positives. (Session 1, 2026-03-06)
  - Source: Issue #1010 - PR #511 tests covered only RHS literals. User directive: "be aggressive about testing for other boundary cases."
  - Citation: `tests/Moq.Analyzers.Test/LinqToMocksExpressionShouldBeValidAnalyzerTests.cs`

- When AI generates both implementation and tests in one PR, require human reviewer to add adversarial boundary cases covering each RHS category (literal, local variable, static member, const field, method call) before merge. AI and its tests share the same blind spots. (Session 1, 2026-03-06)
  - Source: Retrospective - Copilot-authored PR #511 had implementation and tests with shared blind spot. User emphasis: "why YET ANOTHER defect escaped"

- All expression-walking analyzers must follow the upstream three-phase pattern: (1) Register for an OperationKind, (2) Guard by checking operation.Instance to confirm the receiver is the target entity (null = static, skip), (3) Validate the member. Moq1302 skipped phase 2. This is the structural root cause. (Session 1, 2026-03-06)
  - Source: Research agent - dotnet/roslyn-analyzers patterns

## Patterns (MED confidence)

- Analyzers processing expression subtrees must distinguish semantic role of each node. In binary/chained expressions, operands have different purposes. Treating them symmetrically causes false positives. Check: is this operation rooted in the lambda parameter before flagging? (Session 1, 2026-03-06)
  - Source: Retrospective - 4 prior false-positive fixes (PRs #301, #886, #895, #919) shared this pattern.
  - Citation: `src/Analyzers/LinqToMocksExpressionShouldBeValidAnalyzer.cs` AnalyzeLambdaBody IBinaryOperation case

- Upstream uses IOperationExtensions.GetCaptures to scope lambda traversal to only captured symbols. This prevents outer-scope bleed. Evaluate adopting for Mock.Of lambda analysis. (Session 1, 2026-03-06)
  - Source: Research agent - Meziantou IOperation guide

## Edge Cases (MED confidence)

- For analyzers validating Mock.Of expressions: the right-hand side of comparisons can be: StatusCodes.Status200OK, TimeSpan.Zero, MyEnum.Active, config.Property, Defaults.GetValue(). These are value expressions, not mock setups. (Session 1, 2026-03-06)
  - Source: Issue #1010 - analyzer flagged StatusCodes.Status200OK as invalid mock member.

- Thread shared analyzer context (e.g., MoqKnownSymbols) through entire call chains. Creating instances per-operation causes unnecessary allocation. Create once at the entry point (e.g., AnalyzeInvocation) and pass through parameters. (Session 1, 2026-03-06)
  - Source: Efficiency review found MoqKnownSymbols allocated per-call in AnalyzeMemberOperations.
  - Citation: `src/Analyzers/LinqToMocksExpressionShouldBeValidAnalyzer.cs` AnalyzeInvocation

- comment-analyzer agent catches real defects: misleading comments (e.g., "the other side" implying singular when both operands are analyzed) and incomplete XML docs (e.g., missing "event" in member list). Treat comment accuracy gaps as bugs worth fixing. (Session 1, 2026-03-06)
  - Source: PR review found 2 accuracy issues in LinqToMocksExpressionShouldBeValidAnalyzer and IOperationExtensions.

## See Also

- `moq1302-retrospective` - Five Whys analysis, fishbone, skill candidates
- `roslyn-analyzer-best-practices` - Three-phase pattern, upstream testing practices
- `quality-standards` - Project-wide quality requirements
