# Skill Sidecar Learnings: Codebase Analysis and Memory Management

**Last Updated**: 2026-03-15
**Sessions Analyzed**: 1

## Constraints (HIGH confidence)

- Verify issue state (OPEN/CLOSED) before writing memories that claim bugs are active. Cross-reference with `gh issue list --state all`. (Session 1, 2026-03-15)
- Never normalize whitespace in YAML files without running yamllint validation first. Blank lines between mapping keys and sequence values break YAML structure. (Session 1, 2026-03-15)
- Always include infrastructure and configuration in codebase analysis memories. Git hooks, CI workflows, linter configs, build flags, and AI editor rules are as important as code patterns. (Session 1, 2026-03-15)
- When AI reviewer disagrees with codebase convention, verify against actual code before changing. The code is the source of truth, not the reviewer. Example: Gemini suggested CodeFixProvider suffix, but all 5 files use Fixer suffix. (Session 1, 2026-03-15)

## Preferences (MED confidence)

- Use GraphQL batch mutations for resolving multiple PR review threads. One API call replaces N individual calls. Resolved 22 threads in 4 batch calls. (Session 1, 2026-03-15)

## Edge Cases (MED confidence)

- Check for concurrent pushes from other agents before pushing. Use `git pull --rebase` before push. Two push rejections occurred from concurrent remote commits on the same branch. (Session 1, 2026-03-15)
- Cursor/Windsurf AI editor rules with `alwaysApply: true` and auto-fix instructions can cascade across entire repos. Audit AI editor rules before trusting file changes. The Codacy rule caused ~110 file corruption. (Session 1, 2026-03-15)

## Notes for Review (LOW confidence)

- Pre-commit hooks with auto-fix should snapshot dirty files before fixing. The Invoke-AutoFix pattern in LintHelpers.ps1 (dirty-file snapshot, safe-to-stage filtering) is a well-designed safety mechanism worth replicating. (Session 1, 2026-03-15)
