# Forgetful Knowledge Base Reference

This memory is for **cross-machine bootstrapping**. It records what was encoded into Forgetful so other machines can locate existing knowledge rather than re-encoding from scratch.

## Setup

Forgetful MCP must be configured locally:

```bash
claude mcp add forgetful --scope user -- uvx forgetful-ai
```

Storage: `~/.forgetful/forgetful.db` (SQLite, local to each machine).

After adding, restart Claude Code session for `mcp__forgetful__*` tools to appear.

## Moq.Analyzers Project

**Forgetful Project ID**: 27 (environment-specific, stored in `~/.forgetful/forgetful.db` — re-verify after any reset or migration)
**Project Name**: `moq-analyzers`
**Repo**: `rjmurillo/moq.analyzers`
**Encoded**: 2026-03-15

### Memory IDs (511–532)

> **Warning**: These IDs are row identifiers in the local SQLite database. They differ across machines and resets. Re-verify by listing memories for project ID 27 after any migration.

| ID | Title |
|----|-------|
| 511 | Project Purpose and Scope |
| 512 | Tech Stack and Build System |
| 513 | Solution Structure and Organization |
| 514 | Testing Strategy and Infrastructure |
| 515 | Diagnostic ID Scheme |
| 516 | Symbol-Based Detection Pattern |
| 517 | Performance Optimization for IDE Responsiveness |
| 518 | Code Quality Standards and Conventions |
| 519 | Roslyn SDK Dependency and Version Constraints |
| 520 | Transitive Dependency Pinning Strategy |
| 521 | WellKnown Types Pattern for Symbol Resolution |
| 522 | Extension Methods for Roslyn Symbols |
| 523 | MockBehaviorDiagnosticAnalyzerBase Pattern |
| 524 | Code Fix Provider Architecture |
| 525 | Diagnostic Categories and Rule Types |
| 526 | Analyzer Registration and Action Callbacks |
| 527 | Operation Action Registration Pattern |
| 528 | Early Exit Pattern for Performance |
| 529 | Diagnostic Location Precision Pattern |
| 530 | Test Data Pattern with Inline Source Strings |
| 531 | Architecture Overview Document Entry |
| 532 | Contributor Getting Started Guide Entry |

### Document IDs

| ID | Title |
|----|-------|
| 4 | Architecture Overview (5717 bytes) |
| 5 | Contributor Getting Started Guide (7737 bytes) |

### Entity IDs (25–38)

| ID | Name | Type |
|----|------|------|
| 25 | Analyzers Module | CodeModule |
| 26 | Code Fixers Module | CodeModule |
| 27 | Common Utilities Module | CodeModule |
| 28 | WellKnown Types Component | CodeComponent |
| 29 | Extension Methods Component | CodeComponent |
| 30 | Test Infrastructure Module | TestModule |
| 31 | PerfDiff Tool | CLITool |
| 32 | Benchmark Suite | TestModule |
| 33 | NoSealedClassMocksAnalyzer | Analyzer |
| 34 | CallbackSignatureShouldMatchMockedMethodAnalyzer | Analyzer |
| 35 | ConstructorArgumentsShouldMatchAnalyzer | Analyzer |
| 36 | SetExplicitMockBehaviorFixer | CodeFix |
| 37 | CallbackSignatureShouldMatchMockedMethodFixer | CodeFix |
| 38 | Central Package Management | BuildComponent |

## Querying on a New Machine

After configuring Forgetful:

```text
mcp__forgetful__execute_forgetful_tool("query_memory", {
  "query": "moq analyzer architecture",
  "query_context": "understanding project structure",
  "project_ids": [27]
})
```

Or list all project memories:

```text
mcp__forgetful__execute_forgetful_tool("get_project", {"project_id": 27})
```
