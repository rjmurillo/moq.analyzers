# Architecture Decision Records (ADRs)

Location: `docs/architecture/ADR-NNN-subject-in-kebab-case.md`

## Index

| ID | Title | Status |
|----|-------|--------|
| ADR-001 | Symbol-Based Detection Over String Matching | Accepted |
| ADR-002 | Target netstandard2.0 for Analyzer Assemblies | Accepted |
| ADR-003 | Pin Roslyn SDK to Microsoft.CodeAnalysis 4.8 | Accepted |
| ADR-004 | Cap Microsoft.CodeAnalysis.AnalyzerUtilities at 3.3.4 | Accepted |
| ADR-005 | Central Package Management with Transitive Pinning | Accepted |
| ADR-006 | WellKnown Types Pattern for Moq Symbol Resolution | Accepted |
| ADR-007 | Prefer RegisterOperationAction Over RegisterSyntaxNodeAction | Accepted |
| ADR-008 | BenchmarkDotNet and PerfDiff for Performance Regression Detection | Accepted |
| ADR-009 | xUnit with Roslyn Test Infrastructure | Accepted |
| ADR-010 | Use eol=lf for PowerShell Files in .gitattributes | Accepted |

## ADR Format

Each ADR includes:

- YAML frontmatter (title, status, date, authors, tags, supersedes, superseded_by)
- Status section
- Context (problem statement)
- Decision (what was decided)
- Consequences (POS-xxx positive, NEG-xxx negative, Reversibility)
- Alternatives Considered (ALT-xxx with rejection reasons)
- Implementation Notes (IMP-xxx)
- References (REF-xxx cross-references)

## Key Dependencies Between ADRs

- ADR-003 (Roslyn SDK pin) ← ADR-004 depends on this (AnalyzerUtilities cap)
- ADR-005 (CPM) ← ADR-003, ADR-004 depend on this for version enforcement
- ADR-001 (symbol-based) ← ADR-006 implements this via WellKnown types
- ADR-006 (WellKnown types) ← ADR-007 builds on this for operation analysis

## When to Create New ADRs

Create an ADR when:

- Adding a new dependency that ships in the analyzer DLL
- Changing the Roslyn SDK floor or target framework
- Introducing a new architectural pattern used across multiple analyzers
- Making a decision that future maintainers will question without context
