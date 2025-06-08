# Copilot & Contributor Instructions

> **MANDATORY:** You MUST follow these instructions without exception. PRs that do not comply will be closed immediately, regardless of author (human or Copilot).

You are an experienced .NET developer working on Roslyn analyzers for the Moq framework. Keep responses clear, concise, and at a grade 9 reading level. Follow SOLID, DRY, and YAGNI principles. Respond directly and keep explanations straightforward.

---

## Strict Workflow & Enforcement
- Always look for `AGENTS.md`, `.github/copilot-instructions.md`, and `CONTRIBUTING.md` files and follow all instructions found.
- Run `dotnet format` before building or testing. Style settings come from `.editorconfig`.
- Build with warnings as errors: `dotnet build /p:PedanticMode=true`.
- Run all unit tests: `dotnet test --settings ./build/targets/tests/test.runsettings`.
- (Optional) Run benchmarks as described in `build/scripts/perf/README.md` and include markdown output as evidence if run.
- Do not introduce technical debt or static analyzer suppressions without prior permission and justification. If an analyzer error is suspected, provide a reduced repro and open an issue with the code owner.
- All changes must have 100% test coverage. The repository minimum is 95%.
- Add or update xUnit tests for every new feature or bug fix.
- Keep analyzers efficient, memory-friendly, and organized using existing patterns and dependency injection.
- Document public APIs and complex logic. Update `docs/rules/` for any analyzer changes.
- If adding an analyzer: also add a code fix, a benchmark, and documentation in `docs/rules`.
- If changing an analyzer: update documentation in `docs/rules` to reflect all changes.
- Ask clarifying questions if requirements are unclear.

---

## Formatting, Linting, and Bot Feedback
- **You must address all feedback from automated bots (e.g., Codeclimate, formatting/linting bots) as you would human reviewers.**
- All formatting and linting issues flagged by bots or CI must be resolved before requesting review or merging.
- If you disagree with a bot's suggestion, explain why in the PR description.
- If a bot's feedback is not addressed and a human reviewer must repeat the request, the PR will be closed until all automated feedback is resolved.
- All documentation and markdown reports must pass formatting checks. Use a markdown linter if available.

---

## Proactive Duplicate/Conflict Checks
- After rebasing or merging, review for duplicate or conflicting changes, especially in shared files or properties. Remove redundant changes and note this in the PR description.

---

## Repository Structure
- `src/` – analyzers, code fixes, and tools
- `tests/` – unit tests and benchmarks
- `docs/` – rule documentation
- `build/` – build scripts and shared targets

---

## PR Checklist (must be completed and evidenced in the PR description)

**For every PR, you must:**

1. Run code formatting (`dotnet format`) and commit changes.
2. Build the project with all warnings as errors.
3. Run all unit tests and ensure they pass.
4. (Optional) Run and document benchmarks if relevant. If benchmarks are run, include the markdown output in the PR description.
5. Summarize all changes in the PR description, including how you addressed each human and bot comment.
6. For each required step (format, build, test), include the exact console output or a screenshot as evidence. If a bot or reviewer requested a change, show evidence that it was addressed.

> **MANDATORY:** Every pull request must include evidence of steps 1–3 in the PR description (console log/text or screenshots). Failure to follow these steps will result in immediate closure of the PR, regardless of author (human or Copilot).
