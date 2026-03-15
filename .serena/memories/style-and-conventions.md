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
- Prefer `RegisterOperationAction` over `RegisterSyntaxNodeAction` (IOperation tree provides resolved symbols)
- Always call `context.EnableConcurrentExecution()` in `Initialize`
- Create `MoqKnownSymbols` once at analysis entry point; pass through parameters (not per-operation)

## Code Fix Conventions

- Each fixer lives in its own file in `src/CodeFixes/`
- Fixers must specify `FixableDiagnosticIds`
- Use `SyntaxGenerator` for creating new syntax nodes (not direct syntax construction)

## Testing

- Tests use Roslyn's `CSharpAnalyzerVerifier<T>` / `CSharpCodeFixVerifier<TAnalyzer, TFix>`
- Test data uses inline source strings with diagnostic marker syntax: `{|Moq1XXX:CodeHere|}`
  - Example: `var mock = new Mock<{|Moq1001:SealedClass|}>();`
- Each analyzer must have corresponding test class
- Must include both positive cases (diagnostic fires) and negative cases (no false positives)
- Edge cases required: generics, inheritance, async, null values, params arrays

## Diagnostic Metadata

- Each `DiagnosticDescriptor` must include a `helpLinkUri` pointing to `docs/rules/{id}.md`
- Category is typically `"Moq.Usage"`
- Default severity is `Warning` for most diagnostics

## Build Properties

- `PedanticMode=true` enables strict warnings-as-errors
- Central Package Management in `Directory.Packages.props`
- Transitive pins constrained by analyzer host compatibility (.NET 8 SDK)
