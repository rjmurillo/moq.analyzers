# Forgetful Knowledge Base Reference

This memory documents that project knowledge has been encoded into Forgetful. Each contributor must set up Forgetful locally and query their own database to retrieve this knowledge.

## Setup

Forgetful MCP must be configured locally:

```bash
claude mcp add forgetful --scope user -- uvx forgetful-ai
```

Storage: `~/.forgetful/forgetful.db` (SQLite, local to each machine).

After adding, restart Claude Code session for `mcp__forgetful__*` tools to appear.

## Moq.Analyzers Project

**Project Name**: `moq-analyzers`
**Repo**: `rjmurillo/moq.analyzers`

### Encoded Knowledge Topics

The following topics have been encoded into Forgetful for this project:

- Project Purpose and Scope
- Tech Stack and Build System
- Solution Structure and Organization
- Testing Strategy and Infrastructure
- Diagnostic ID Scheme
- Symbol-Based Detection Pattern
- Performance Optimization for IDE Responsiveness
- Code Quality Standards and Conventions
- Roslyn SDK Dependency and Version Constraints
- Transitive Dependency Pinning Strategy
- WellKnown Types Pattern for Symbol Resolution
- Extension Methods for Roslyn Symbols
- MockBehaviorDiagnosticAnalyzerBase Pattern
- Code Fix Provider Architecture
- Diagnostic Categories and Rule Types
- Analyzer Registration and Action Callbacks
- Operation Action Registration Pattern
- Early Exit Pattern for Performance
- Diagnostic Location Precision Pattern
- Test Data Pattern with Inline Source Strings
- Architecture Overview Document
- Contributor Getting Started Guide

### Encoded Entities

The following code entities have been indexed:

- Analyzers Module
- Code Fixers Module
- Common Utilities Module
- WellKnown Types Component
- Extension Methods Component
- Test Infrastructure Module
- PerfDiff Tool
- Benchmark Suite
- NoSealedClassMocksAnalyzer
- CallbackSignatureShouldMatchMockedMethodAnalyzer
- ConstructorArgumentsShouldMatchAnalyzer
- SetExplicitMockBehaviorFixer
- CallbackSignatureShouldMatchMockedMethodFixer
- Central Package Management

## Querying on a New Machine

First, list projects to find your local project ID:

```text
mcp__forgetful__execute_forgetful_tool("list_projects", {})
```

Then query memories using your local project ID:

```text
mcp__forgetful__execute_forgetful_tool("query_memory", {
  "query": "moq analyzer architecture",
  "query_context": "understanding project structure",
  "project_ids": [<your_local_project_id>]
})
```

Or list all project memories:

```text
mcp__forgetful__execute_forgetful_tool("get_project", {"project_id": <your_local_project_id>})
```
