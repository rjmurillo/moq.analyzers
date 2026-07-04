---
name: moq-analyzers-docs-and-writing
description: Maintains moq.analyzers documentation and writing standards. Load when adding or editing docs/rules/MoqXXXX.md pages, the README rule tables, ADRs under docs/architecture/, AnalyzerReleases files, CONTRIBUTING.md, or .github/copilot-instructions.md; when writing commit messages, PR descriptions, or release notes; when deciding which doc is the source of truth for a fact; or when a markdownlint/yamllint/super-linter gate fails on a doc change. Keywords - rule doc template, ADR template, docs update trigger, Conventional Commits, release-drafter, NuGet description, stale docs. Do NOT load for the mechanics of shipping a release or editing AnalyzerReleases.Shipped.md during release promotion (see moq-analyzers-rule-lifecycle), for CI/build setup (see moq-analyzers-build-and-env), or for writing analyzer code itself (see roslyn-analyzer-reference).
---

# moq.analyzers: docs of record, house style, and external positioning

This skill covers WHAT documentation exists, WHICH file owns each fact, the exact
templates to copy, the writing rules that gates enforce, and what the project may
publicly claim about itself. All paths are repo-root relative. All volatile facts
are stamped 2026-07-02 and re-verifiable via the Provenance section at the end.

Context for zero-context readers: this repo ships `Moq.Analyzers`, a NuGet package
of Roslyn analyzers (compiler plugins that flag misuse of the Moq mocking library
at build time). Docs here are user-facing product surface — a wrong rule doc
misleads users of mission-critical codebases, so docs get the same rigor as code.

## 1. Docs inventory: owner-of-truth per fact

When two documents disagree, the owner below wins. Fix the non-owner, via a normal
PR (see moq-analyzers-change-control — never bypass review to "quickly fix docs").

| Fact | Owner of truth | Mirrors that must be kept in sync |
| --- | --- | --- |
| Rule IDs and which exist (25 IDs, Moq1000–Moq1600; Moq1209 intentionally reserved) | `src/Common/DiagnosticIds.cs` (the reservation comment lives ONLY here) | `docs/rules/README.md` table, root `README.md` table (both have 25 rows, verified 2026-07-02) |
| Per-rule behavior, examples, suppression | `docs/rules/Moq{Id}.md` (one page per ID, 25 files) | Analyzer `helpLinkUri` points at it (see §2) |
| Rule category + ID-range allocation | `docs/rules/README.md` §"Guidance for Future Rules" | `src/Common/DiagnosticIds.cs` ordering; each analyzer's `DiagnosticDescriptor` category |
| Release-tracking record of shipped/changed rules | `src/Analyzers/AnalyzerReleases.Shipped.md` (immutable) + `AnalyzerReleases.Unshipped.md` (pending) | None — this pair IS the record; build fails (RS2000 family) if a new rule is missing from Unshipped |
| Architecture decisions | `docs/architecture/ADR-001` … `ADR-010` (10 files, 2026-07-02) | `.serena/memories/` summaries (informal, may be stale) |
| Contributor workflow, commit format, release process | `CONTRIBUTING.md` | `.github/copilot-instructions.md` and `.github/instructions/*` (self-contained AI copies; drift is a known hazard, see §7) |
| AI-agent hard constraints | `.github/copilot-instructions.md` (root `AGENTS.md` just includes it via `@.github/copilot-instructions.md`) | `.github/instructions/*.instructions.md` per file type |
| Dependency-update policy | `docs/dependency-management.md` (Renovate is the sole bot) | — |
| NuGet package metadata (description, license, tags) | `Directory.Build.props` (`<Description>`) + `src/Analyzers/Moq.Analyzers.csproj` (package metadata block) | Root `README.md` is packed into the nupkg (`PackageReadmeFile`), so README claims ARE NuGet-page claims |
| Release notes | GitHub Releases (drafted by `.github/release-drafter.yml`, hand-polished before publish) | `<PackageReleaseNotes>` links to the releases page |
| Benchmark how-to | `build/scripts/perf/README.md` (canonical entry: `build/scripts/perf/PerfCore.ps1`) | `CONTRIBUTING.md` §Performance Testing points there |

Rule of thumb: code and release-tracking files own machine-checked facts; docs own
explanations. Never state a number (rule count, overload count, version) in a doc
without re-deriving it from the owner file first.

## 2. Doc-update triggers (the enforced rules, quoted)

`.github/copilot-instructions.md` makes doc updates mandatory, not optional:

- "Update `docs/rules/` and `README.md` for any analyzer, code fix, or workflow
  change" (Quick Reference table)
- "Update `src/Analyzers/AnalyzerReleases.Unshipped.md` and add or update
  documentation in `docs/rules/` for each diagnostic."
- "Update `README.md` and `docs/rules/README.md` if workflows or rules change."

`CONTRIBUTING.md` §"Documentation Standards" requires documentation updates for:
new analyzers or fixers; changes to existing analyzer behavior; API changes;
installation or usage changes; CI/CD workflow changes.

Checklist — a PR that touches an analyzer or fixer must also touch:

- [ ] `docs/rules/Moq{Id}.md` (new page, or update if behavior changed)
- [ ] `docs/rules/README.md` master table (new row: ID, category, title, link to
      the implementation file)
- [ ] Root `README.md` rule table (same row, minus the implementation link)
- [ ] `src/Analyzers/AnalyzerReleases.Unshipped.md` — ONLY for a new rule or an
      ID/category/severity **metadata** change; a pure logic/span/FP fix to a
      shipped rule needs NO release row (see §5 and moq-analyzers-rule-lifecycle
      Part 3; never touch `Shipped.md` outside release promotion)
- [ ] Tests and PR evidence (owned by moq-analyzers-validation-and-qa)

The analyzer's `helpLinkUri` must follow the repo pattern (all 24 analyzer classes
use it, verified 2026-07-02):

```csharp
helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.YourRuleId}.md");
```

Note it pins to `ThisAssembly.GitCommitId`, not `main` — so the doc page must exist
in the same commit that ships the analyzer, or the link 404s for users.

## 3. Per-rule doc template (copy this skeleton)

Extracted from `docs/rules/Moq1600.md` and `docs/rules/Moq1400.md` (the current
house pattern, 2026-07-02). Replace `Moq9999` throughout. Keep every section; the
"When this rule does not apply" section is optional but strongly encouraged (it is
where you pre-empt false-positive reports).

````markdown
# Moq9999: Imperative one-line title matching the descriptor

| Item     | Value   |
| -------- | ------- |
| Enabled  | True    |
| Severity | Warning |
| CodeFix  | False   |

---

One or two short paragraphs: what mistake this catches and why it matters at
runtime (what Moq does when you get it wrong). To fix:

- Bullet the concrete remediations.

## Examples of patterns that are flagged by this analyzer

```csharp
// Minimal, COMPILING setup code (interfaces/classes the example needs)

var mock = new Mock<IMyService>();
mock.Something(); // Moq9999: repeat the diagnostic title here
```

## Solution

```csharp
// The same example, corrected. No diagnostic comment.
```

## When this rule does not apply

```csharp
// A near-miss pattern that is CORRECT and must not trigger the rule.
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable Moq9999
mock.Something();
#pragma warning restore Moq9999
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.{cs,vb}]
dotnet_diagnostic.Moq9999.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).
````

Template rules:

| Rule | Why |
| --- | --- |
| Metadata table has exactly three rows: Enabled, Severity, CodeFix | Every existing page uses this; tooling and readers rely on it |
| Flagged lines carry a trailing `// MoqXXXX: <title>` comment | This is how readers map code to diagnostic; mirror the exact descriptor title |
| Example code must compile against real Moq | Same standard as test code — plausible-but-wrong snippets are the project's costliest failure mode. Paste the example into a scratch test with `AnalyzerVerifier` if unsure |
| Severity/CodeFix values must match the `DiagnosticDescriptor` and whether a fixer class exists in `src/CodeFixes/` | 5 rules have `CodeFix: True` today: Moq1100, Moq1208, Moq1210, Moq1400, Moq1410 (2026-07-02) |
| Suppress section is boilerplate — copy it verbatim, swap the ID | Consistency across 25 pages |

## 4. ADR template (copy this skeleton)

ADR = Architecture Decision Record: a short document that captures one
irreversible-ish technical decision so nobody relitigates it from scratch.
Extracted from `docs/architecture/ADR-001-symbol-based-detection-over-string-matching.md`;
all 10 ADRs share this structure including the tagged-ID convention (verified
2026-07-02). File name: `docs/architecture/ADR-0NN-kebab-case-title.md`.

```markdown
---
title: "ADR-0NN: Title In Title Case"
status: "Accepted"
date: "YYYY-MM-DD"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "..."]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

What forces are in play. Plain statements of fact; define jargon.

## Decision

One paragraph, present tense, declarative: "All analyzers use ...".

## Consequences

### Positive

- **POS-001**: ...
- **POS-002**: ...

### Negative

- **NEG-001**: ...

## Alternatives Considered

### Name of Alternative

- **ALT-001**: **Description**: ...
- **ALT-002**: **Rejection Reason**: ...

## Implementation Notes

- **IMP-001**: ...

## References

- **REF-001**: `src/...` -- what it shows
```

The `POS-/NEG-/ALT-/IMP-/REF-` numbered IDs are house convention — they let PRs
and issues cite a single consequence ("per ADR-001 NEG-001"). Keep them. Existing
ADRs are settled doctrine; changing one requires `supersedes`/`superseded_by`
linkage and maintainer sign-off, not an in-place rewrite (see
moq-analyzers-change-control and moq-analyzers-failure-archaeology for the
battles behind ADR-001, -003, -004, -010).

## 5. AnalyzerReleases files (release-tracking docs)

These two files in `src/Analyzers/` are consumed by Roslyn's release-tracking
analyzers (RS2000 family): a new diagnostic ID that is not listed FAILS THE BUILD.

Hard rules (from `.github/copilot-instructions.md`, enforced by review):

- `AnalyzerReleases.Shipped.md` is an immutable record of past releases. Edit it
  ONLY during release promotion (move Unshipped rows under a new `## Release X.Y.Z`
  heading and clear Unshipped — see `CONTRIBUTING.md` §Release Process and the
  moq-analyzers-rule-lifecycle skill).
- New rule → add a row to the `### New Rules` table in `Unshipped.md`:
  `Moq9999 | Category | Severity | YourAnalyzerClassName`
- Category values in current use: `Usage`, `Correctness`, `Best Practice`
  (aligned with the README tables by PR #1087, which closed issue #944 on
  2026-03-15 — the old "everything says Usage" inconsistency is FIXED).

KNOWN INCONSISTENCY (2026-07-02): `.github/copilot-instructions.md` states
Unshipped "must only contain a `### New Rules` section", but the actual
`AnalyzerReleases.Unshipped.md` on `main` contains both `### New Rules` (Moq1003,
Moq1004, Moq1208, Moq1600) and a `### Changed Rules` section (the category
realignment from PR #1087), and the build passes with it. **Do not treat the live
file's shape as authoritative and do not append to `### Changed Rules`.** The root
`.github/copilot-instructions.md` (loaded via `AGENTS.md`) is a higher-priority
policy than this skill, so when you record a rule change, add it as a `### New
Rules` row per that policy. Leave the pre-existing `### Changed Rules` block in
place (the upstream Roslyn format tolerates it, which is why the build passes) but
never restructure it in an unrelated PR. Because the on-disk file and the written
policy genuinely diverge, this is a STOP-and-flag: state it in your PR and let the
maintainer reconcile the data file — never resolve it silently. The full runbook
and rationale live in `moq-analyzers-rule-lifecycle` §Part 3; this note defers to it.

Note: super-linter explicitly excludes `AnalyzerReleases.(Shipped|Unshipped).md`
from markdown linting (`.github/workflows/linters.yml`, `FILTER_REGEX_EXCLUDE`) —
their table format is dictated by Roslyn, not by markdownlint.

## 6. House style and writing gates

### Prose style

- Target a grade 9 reading level, plain English, no unexplained jargon
  (`.github/copilot-instructions.md`: "Keep responses clear, concise, and at a
  grade 9 reading level. Use plain English, avoid jargon.").
- Imperative voice in docs and commit subjects ("Add", "Guard", not "Added").
- Define every term at first use; prefer tables and checklists over paragraphs.
- Never narrate success — show evidence (logs, test output).

### Commit messages (Conventional Commits)

Format (`CONTRIBUTING.md` §Commit Message Format):

```text
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

- Types: `feat` `fix` `docs` `style` `refactor` `test` `chore` `ci` `perf`
  `build` `revert`. Subject is all lowercase after the type, imperative mood.
- Length: subject ideally ≤ 50 characters; body wrapped at 72
  (`CONTRIBUTING.md`: "The first line should ideally be no longer than 50
  characters, and the body should be restricted to 72 characters.").
- Footer links issues: `Closes #42`. Breaking changes: `BREAKING CHANGE: ...`.
- PR titles are gated by the `semantic-pr-check` workflow — a non-conforming
  title blocks merge. Docs-only changes use `docs:` and branch `docs/issue-{n}`.

### PR descriptions

Use `.github/pull_request_template.md` (Description / Motivation and Context /
Changes Made / Type of Change / Checklist). Every PR must additionally embed
evidence blocks (`.github/copilot-instructions.md` §Required Validation Evidence):

- Output of `dotnet format`, `dotnet build`, `dotnet test`, and Codacy analysis
  (logs or screenshots).
- A Moq version compatibility note for analyzer/test changes (4.8.2 vs 4.18.4).
- For `.github/workflows/**` changes: local `actionlint` output (and act-based
  dry-run evidence) per `CONTRIBUTING.md` §Workflow File Validation.

### Lint gates that police docs (what will actually fail your PR)

| Gate | Where it runs | Config | Notes |
| --- | --- | --- | --- |
| `markdownlint-cli2` | Husky.NET pre-commit hook (`.husky/task-runner.json`), on staged `**/*.md` | `.markdownlint.json` | Disabled rules: MD013 (line length), MD024 (duplicate headings), MD033 (inline HTML), MD041 (first-line heading), MD060. Everything else is default-on |
| `yamllint` | pre-commit, staged `**/*.yml`/`*.yaml` | `.yamllint.yml` | extends default; line length max 320; `document-start` disabled; truthy keys not checked |
| super-linter v8.7.0 | CI `linters.yml` (`VALIDATE_MARKDOWN: true`, `VALIDATE_YAML: true`, ...) | same configs | `FILTER_REGEX_EXCLUDE` skips `*.verified.*` snapshots, `AnalyzerReleases.*.md`, and `.agents/` (the `.cursor/`, `.github/chatmodes/`, `.windsurf/` excludes were dropped when #1215 removed those dirs, 2026-07-04) |
| actionlint | pre-commit + CI | — | workflows only |

Practical consequence: long lines in Markdown are fine (MD013 off), but heading
hierarchy, list indentation, fenced-code language tags, and trailing spaces are
enforced. Run before pushing:

```bash
markdownlint-cli2 "docs/**/*.md" "README.md"
yamllint --config-file .yamllint.yml .github/workflows/
```

### XML doc comments (C# API docs)

Required for all public APIs (`CONTRIBUTING.md` §XML Documentation Standards):
`<see cref="Task{T}"/>` for type references, `<see langword="true"/>` for
keywords, `<paramref name="x"/>` for parameters, `<c>x => x.Method()</c>` for
inline code. Plain-text type names in XML docs are rejected in review.

## 7. Known stale docs — fix or distrust (snapshot 2026-07-02)

These are verified drift points. When you touch a neighboring file, fix them via
normal change control; until then, do not propagate their claims.

| Location | Stale claim | Ground truth |
| --- | --- | --- |
| `.serena/memories/complete-analyzer-catalog.md` | "24 Diagnostic Rules"; omits Moq1600 entirely | 25 rule IDs (`src/Common/DiagnosticIds.cs`), 24 `[DiagnosticAnalyzer]` classes (ConstructorArgumentsShouldMatchAnalyzer owns both Moq1001 and Moq1002); Moq1600 shipped via PR #1088 |
| `.github/instructions/project.instructions.md` | "Target .NET 8, C# 12 by default"; elsewhere "target framework (.NET 9)" | Dev toolchain is .NET 10 SDK / C# 14 per `global.json` and `.github/copilot-instructions.md` Quick Reference; shipped analyzers still target netstandard2.0 (that part is correct) |
| `.github/copilot-instructions.md` (AnalyzerReleases paragraph) | Unshipped.md "must only contain a `### New Rules` section" | Actual `Unshipped.md` on `main` also carries `### Changed Rules` since PR #1087; build is green. See §5 |
| Benchmark how-to (multiple variants across docs/memories) | Divergent invocation instructions | Canonical: `./build/scripts/perf/PerfCore.ps1 -projects "<path>" [-filter "<glob>"] [-diff]` per `build/scripts/perf/README.md`; root `Perf.sh`/`Perf.cmd` and `build/scripts/perf/CIPerf.sh`/`.cmd` are thin wrappers. `CONTRIBUTING.md` correctly points at PerfCore.ps1 |
| Issue #944 sometimes cited as open doc debt | "Unshipped categories all say Usage" | CLOSED as completed 2026-03-15 (fixed by PR #1087); do not re-file |

General rule: `.serena/memories/` files are convenience summaries, not docs of
record. Anything load-bearing must be re-verified against `src/`, `docs/`, or
`build/` before you repeat it.

## 8. External positioning: what we may claim, and where

The root `README.md` is packed into the NuGet package (`PackageReadmeFile` in
`src/Analyzers/Moq.Analyzers.csproj`), so every README sentence is a public
product claim on nuget.org. Discipline:

### Safe to claim (verifiable from the repo, 2026-07-02)

- 25 analyzer rules, Moq1000–Moq1600 (both README tables list all 25).
- License BSD-3-Clause; package id `Moq.Analyzers`; development-dependency
  package (no runtime footprint in consumers' apps).
- Analyzer assemblies target netstandard2.0 and load in .NET 8 analyzer hosts
  (VS 2022 17.8+) — this is triple-enforced in CI (host-compat MSBuild target,
  DLL reference check, 9-way analyzer-load-test matrix; see
  moq-analyzers-build-and-env).
- Tested against Moq 4.8.2 and 4.18.4 (every test row fans across both).
- Current package `<Description>` (owner: `Directory.Build.props`): "Roslyn
  analyzer that helps to write unit tests using Moq mocking library by
  highlighting typical errors and suggesting quick fixes. Port of Resharper
  extension to Roslyn. Find the full list of detected issues at project GitHub
  page."

### Requires proof before claiming

- Any "no false positives" / "zero FP" wording. The 2026-07-02 audit has open
  confirmed FP findings (e.g. the canonical `mock.Raise(m => m.Closed += null,
  EventArgs.Empty)` pattern, issues #1241–#1278 campaign). A zero-FP claim needs
  the real-world corpus validation described in moq-analyzers-research-frontier
  and the moq-analyzers-fp-convergence-campaign to converge first.
- Performance numbers ("negligible IDE overhead" etc.) — only with PerfDiff/BDN
  evidence (moq-analyzers-proof-toolkit).
- "Catches all Moq misuse" — known false negatives exist (non-generic `Setup`
  overload, target-typed `new`); never claim completeness.

Ecosystem context (label as positioning, not a verified metric): this is the
community-maintained Moq analyzer package (Moq itself does not ship one); its
reach argument is the netstandard2.0 target plus the .NET 8-host compatibility
guarantee. Do not quote download counts or "the most popular" without pulling
live numbers from nuget.org at writing time.

### Release notes

`.github/release-drafter.yml` auto-drafts a GitHub release on every merge to
`main`, categorized by PR labels (autolabeled from Conventional Commit titles:
`feat:`→feature, `fix:`→bug, `docs:`→documentation, ...). The config header says
what to do before publishing — quote:

> The draft is a starting point: before publishing a release, the maintainer
> should: 1. Create a release branch (release/vX.Y.Z) per CONTRIBUTING.md
> 2. Retarget the draft release to that branch 3. Set the tag to match the
> NBGV-derived version (e.g. v0.5.0) 4. Polish the notes to match the project's
> release style (see v0.4.0, v0.4.1 for examples with Highlights, Breaking
> Changes, Contributors, Resources, and Feedback sections)

Versioning is controlled by Nerdbank.GitVersioning (`version.json`, currently
`0.5.0-alpha.{height}`), NOT by release-drafter. Drafting notes is a writing
task (this skill); executing the release is moq-analyzers-rule-lifecycle +
moq-analyzers-change-control territory.

## 9. Worked example: doc set for a new rule (traced from Moq1600, PR #1088)

1. `src/Common/DiagnosticIds.cs` — added
   `ProtectedSetupUsesItMatcherInsteadOfItExpr = "Moq1600"` in the 1600–1699
   (protected-member Usage) range.
2. `src/Analyzers/ProtectedSetupShouldUseItExprAnalyzer.cs` — descriptor
   `helpLinkUri` points at `docs/rules/Moq1600.md` via `ThisAssembly.GitCommitId`.
3. `src/Analyzers/AnalyzerReleases.Unshipped.md` — row
   `Moq1600 | Usage | Warning | ProtectedSetupShouldUseItExprAnalyzer`.
4. `docs/rules/Moq1600.md` — full template of §3, including a
   "When this rule does not apply" section covering the lambda-based
   `.Protected().As<T>()` API that legitimately uses `It`.
5. `docs/rules/README.md` — row with category `Usage`, title
   "Protected setup should use `ItExpr` matchers", link to the analyzer file.
6. Root `README.md` — matching row.

All six landed in one PR with tests. That is the reference shape for any new
rule's documentation footprint.

## When NOT to use this skill

- Executing a release or promoting Unshipped→Shipped: **moq-analyzers-rule-lifecycle**
  (this skill only covers the docs' format and note drafting).
- Deciding whether a change is allowed at all, branch/merge policy, bot-feedback
  protocol: **moq-analyzers-change-control**.
- Build/test/toolchain setup, PedanticMode, hook installation: **moq-analyzers-build-and-env**.
- Writing or debugging analyzer code: **roslyn-analyzer-reference**,
  **moq-analyzers-debugging-playbook**, **moq-analyzers-architecture-contract**.
- Moq API semantics for examples you're documenting: **moq-api-reference**.
- Rule severities/flags configuration behavior: **moq-analyzers-config-and-flags**.
- Producing the evidence blocks a PR must embed: **moq-analyzers-validation-and-qa**
  and **moq-analyzers-proof-toolkit**.
- History of why a doc says what it says (settled battles): **moq-analyzers-failure-archaeology**.
- Corpus validation needed before stronger public claims: **moq-analyzers-research-frontier**,
  **moq-analyzers-fp-convergence-campaign**, **moq-analyzers-research-methodology**.
- Interpreting analyzer diagnostics tooling output: **moq-analyzers-diagnostics-and-tooling**.
- BCL/API-design style for public C# surface: **dotnet-api-design-standards**.

## Provenance and maintenance

Re-verify before trusting any volatile fact above:

- Rule count and tables in sync: `grep -c '^| \[Moq' README.md docs/rules/README.md && ls docs/rules/Moq*.md | wc -l` (expect 25 / 25 / 25)
- Reserved ID comment: `grep -n 'Moq1209' src/Common/DiagnosticIds.cs`
- CodeFix=True pages match fixers: `grep -l 'CodeFix  | True' docs/rules/*.md && ls src/CodeFixes/*Fixer.cs` (expect 5 pages, 5 fixers)
- ADR count/structure: `ls docs/architecture/ADR-*.md | wc -l && grep -l '## Alternatives Considered' docs/architecture/ADR-*.md | wc -l` (expect 10 / 10)
- Unshipped.md current sections: `grep '^###' src/Analyzers/AnalyzerReleases.Unshipped.md`
- helpLinkUri pattern still universal: `grep -L 'ThisAssembly.GitCommitId' src/Analyzers/*Analyzer.cs` (expect empty)
- markdownlint disabled rules: `cat .markdownlint.json`
- super-linter exclusions: `grep FILTER_REGEX_EXCLUDE .github/workflows/linters.yml`
- Commit length rule: `grep -n '50 characters' CONTRIBUTING.md`
- Issue #944 state: `gh issue view 944 --json state` (expect CLOSED)
- Stale memory check: `grep -n 'Moq1600' .serena/memories/complete-analyzer-catalog.md` (empty ⇒ still stale)
- Stale instructions check: `grep -n '.NET 8, C# 12' .github/instructions/project.instructions.md`
- Package description owner: `grep -n '<Description>' Directory.Build.props`
- Version stem: `grep '"version"' version.json` (expect `0.5.0-alpha.{height}`)
- Release-drafter behavior: `sed -n 1,20p .github/release-drafter.yml`

Last verified: 2026-07-02 against commit 05135b2.
