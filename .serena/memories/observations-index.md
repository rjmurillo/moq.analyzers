# Observations Index

Cross-cutting observations extracted from retrospectives and session learnings.

## False Positive Patterns

### Expression-tree semantic role confusion

Moq analyzer false positives cluster around expression-tree nodes with multiple semantic roles (binary ops, method chains, parenthesized expressions). Treating operands symmetrically causes false positives on the "value" operand. Prior occurrences: Moq1200 (PR #301), Moq1203 (PRs #886, #895, #919), Moq1302 (Issue #1010).

- Source: moq1302 retrospective, Phase 1 Pattern analysis
- See also: `analyzer-testing-observations.md` Patterns section

### Binary operation operand roles

In binary expressions within lambda analysis, only recurse into the operand referencing the lambda parameter. The other operand is a value expression (literal, local, static member, const, method call) and must not be validated for mockability.

- Source: moq1302 retrospective, Learning 1 (Atomicity 90%)
- Fix: `AnalyzeLambdaBody` IBinaryOperation case in `LinqToMocksExpressionShouldBeValidAnalyzer.cs`

## Test Authorship

### Co-authored implementation and tests share blind spots

When AI generates both implementation and tests in a single PR, the test suite inherits the same assumptions as the implementation. Require independent adversarial boundary cases from a human reviewer covering each RHS category: literal, local variable, static member, const field, and method call.

- Source: moq1302 retrospective, Learning 2 (Atomicity 85%)
- Evidence: PR #511 Copilot-generated impl+tests; RHS static member cases absent until issue #1010

### RHS boundary test checklist for binary comparisons

For any binary/comparison operation in an analyzer, test all RHS types: literal, local variable, static member of unrelated type, const field, method call on unrelated type, nested property chain on unrelated type.

- Source: moq1302 retrospective, Learning 3 (Atomicity 80%)
- Evidence: 5 missing RHS boundary categories allowed issue #1010 to escape

## Process

### Retrospective methodology

The moq1302 retrospective used a structured format: Data Gathering (4-step debrief), Insights (Five Whys, fishbone), Diagnosis (atomicity scoring), Decisions (SMART validation), Extracted Learnings, Recursive Learning Extraction. This format produced 4 atomic skills with atomicity scores >= 75%.

- Source: moq1302 retrospective, Phases 0-6
