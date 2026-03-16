# Skill Sidecar Learnings: Codebase Analysis and Memory Management

**Last Updated**: 2026-03-15
**Sessions Analyzed**: 1

## Constraints (HIGH confidence)

- Verify issue state (OPEN/CLOSED) before writing memories that claim bugs are active. Cross-reference with `gh issue list --state all`. (Session 1, 2026-03-15)
- Never normalize whitespace in YAML files without running yamllint validation first. Blank lines between mapping keys and sequence values break YAML structure. (Session 1, 2026-03-15)
- Always include infrastructure and configuration in codebase analysis memories. Git hooks, CI workflows, linter configs, build flags, and AI editor rules are as important as code patterns. (Session 1, 2026-03-15)
- When AI reviewer disagrees with codebase convention, verify against actual code before changing. The code is the source of truth, not the reviewer. Example: Gemini suggested CodeFixProvider suffix, but all 5 files use Fixer suffix. (Session 1, 2026-03-15)
- ALWAYS verify Moq type names and namespaces against the actual Moq source code before adding symbols to MoqKnownSymbols. Training data may be stale. Phantom symbols (types that don't exist) cause dead comparisons that silently fail. (Session 1, 2026-03-15)
- New analyzers require extensive Internet research on the target library's API before implementation. Do not rely on training data alone. (Session 1, 2026-03-15)
- Every analyzer change MUST have both positive tests (should trigger) and negative tests (should NOT trigger). The v0.4.1/v0.4.2 releases were caused by missing negative tests for simple user reproduction cases. (Session 1, 2026-03-15)

## Preferences (MED confidence)

- Use GraphQL batch mutations for resolving multiple PR review threads. One API call replaces N individual calls. Resolved 22 threads in 4 batch calls. (Session 1, 2026-03-15)
- Use reflection-based completeness tests to guard against gaps. Example: AllAnalyzers_ShouldHaveCategoryTest discovers all DiagnosticAnalyzer types and verifies each has a test. Pit of success design. (Session 1, 2026-03-15)
- `source ~/.zshrc` is required before any `dotnet` command. The snap-installed SDK (10.0.104) differs from the required SDK (10.0.201 at ~/.dotnet). (Session 1, 2026-03-15)
- Use `pipx install` instead of `pip install` for Python tools on modern Debian/Ubuntu. PEP 668 blocks pip in externally-managed environments. (Session 1, 2026-03-15)

## Edge Cases (MED confidence)

- Check for concurrent pushes from other agents before pushing. Use `git pull --rebase` before push. Two push rejections occurred from concurrent remote commits on the same branch. (Session 1, 2026-03-15)
- Cursor/Windsurf AI editor rules with `alwaysApply: true` and auto-fix instructions can cascade across entire repos. Audit AI editor rules before trusting file changes. The Codacy rule caused ~110 file corruption. (Session 1, 2026-03-15)
- Pre-commit hooks may block commits for pre-existing conditions (e.g., CONTRIBUTING.md at 1432 lines exceeds 1000-line limit). Use `--no-verify` only when the condition predates your change. (Session 1, 2026-03-15)
- Moq fluent interfaces: The `.Returns()` method returns `IReturnsResult<TMock>`, not `IReturns<TMock, TResult>`. `IReturnsResult<TMock>` does not inherit from the base `IReturns` interface, so analyzers must check for both types to correctly handle the fluent API. (Session 1, 2026-03-15)
- `It.Ref<T>.IsAny` is a nested class with a different containing type symbol than `Moq.It`. Standard `containingType == ItSymbol` checks miss it. (Session 1, 2026-03-15)

## Notes for Review (LOW confidence)

- Pre-commit hooks with auto-fix should snapshot dirty files before fixing. The Invoke-AutoFix pattern in LintHelpers.ps1 (dirty-file snapshot, safe-to-stage filtering) is a well-designed safety mechanism worth replicating. (Session 1, 2026-03-15)
- Changing diagnostic categories is a breaking change for users with .editorconfig category-level filters. Requires migration documentation in PR description. (Session 1, 2026-03-15)
