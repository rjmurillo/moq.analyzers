# Code Style and Conventions

## General

- Follow .NET Framework Design Guidelines
- C# 13 language features allowed
- Conventional Commits format for git messages (e.g., `feat:`, `fix:`, `chore:`, `docs:`)
- EditorConfig enforces formatting rules (tabs/spaces, line endings, etc.)

## Naming

- PascalCase for public types, methods, properties
- camelCase for local variables and parameters
- Prefix interfaces with `I` (e.g., `ITypeSymbolExtensions`)
- Suffix analyzers with `Analyzer` (e.g., `NoSealedClassMocksAnalyzer`)
- Suffix code fixes with `Fixer` (e.g., `SetExplicitMockBehaviorFixer`)
- Extension method classes named `{Type}Extensions` (e.g., `IMethodSymbolExtensions`)

## Analyzer Conventions

- Each analyzer lives in its own file in `src/Analyzers/`
- Diagnostic IDs follow pattern `Moq1XXX` (see `src/Common/DiagnosticIds.cs`)
- Use `WellKnown` types in `src/Common/WellKnown/` for Moq type references
- Always use Roslyn symbol APIs, never string matching on syntax
- Register actions in `Initialize` method; perform analysis in callbacks

## Code Fix Conventions

- Each fixer lives in its own file in `src/CodeFixes/`
- Fixers must specify `FixableDiagnosticIds`
- Use `SyntaxGenerator` for creating new syntax nodes

## Testing

- Tests use Roslyn's `CSharpAnalyzerVerifier` / `CSharpCodeFixVerifier`
- Test data uses inline source strings
- Each analyzer should have corresponding test class

## Build Properties

- `PedanticMode=true` enables strict warnings-as-errors
- Central Package Management in `Directory.Packages.props`
- Transitive pins constrained by analyzer host compatibility (.NET 8 SDK)
