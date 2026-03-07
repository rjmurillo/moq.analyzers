# Retrospective: Moq1302 False Positive (Issue #1010)

**Date**: 2026-03-06
**Analyzer**: `LinqToMocksExpressionShouldBeValidAnalyzer` (Moq1302)
**Outcome**: False positive shipped in v0.4.0 on `StatusCodes.Status200OK` RHS in binary comparison.

## Summary

The `IBinaryOperation` case in `AnalyzeLambdaBody` unconditionally recursed into both operands. The RHS of `x.Prop == ExternalClass.Constant` is a value expression, not a mock setup target. The analyzer flagged it as an invalid mock member.

**Root cause**: Co-authored implementation and tests (Copilot, PR #511) shared the same blind spot. No test covered RHS = static/const member of a concrete class.

## Key Observations

Extracted to `observations-index.md`:

- Expression-tree semantic role confusion (false positive pattern)
- Binary operation operand roles (technical fix)
- Co-authored impl+tests share blind spots (process)
- RHS boundary test checklist (testing)

## Skill Candidates

| Skill ID | Statement | Atomicity |
|----------|-----------|-----------|
| binary-op-operand-roles | Analyze only the lambda-parameter operand in binary expressions; skip value operands. | 90% |
| adversarial-test-authorship | Require independent adversarial boundary cases when AI generates both implementation and tests. | 85% |
| rhs-boundary-test-checklist | Test all RHS types (literal, local, static member, const, method call) for binary comparison analyzers. | 80% |
| expression-tree-false-positive-pattern | Moq analyzer false positives cluster around binary/multi-role expression-tree nodes. | 75% |

## See Also

- `observations-index.md` - extracted observations
- `analyzer-testing-observations.md` - detailed testing constraints and patterns
- `roslyn-analyzer-best-practices.md` - three-phase analyzer pattern
