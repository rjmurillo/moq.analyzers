#!/usr/bin/env bash
# Tests for classify-changed-paths.sh. Exits non-zero when any case fails.
# Run: bash build/scripts/ci/classify-changed-paths.test.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR
readonly CLASSIFIER="$SCRIPT_DIR/classify-changed-paths.sh"

failures=0
cases=0

# Usage: expect <tier> <case name> <stdin text>
expect() {
  local expected="$1"
  local name="$2"
  local input="$3"
  local actual

  cases=$((cases + 1))
  actual="$(printf '%s' "$input" | bash "$CLASSIFIER")"
  if [[ "$actual" == "$expected" ]]; then
    echo "[PASS] $name"
    return 0
  fi

  echo "[FAIL] $name: expected '$expected', got '$actual'"
  failures=$((failures + 1))
}

# docs-only: nothing reads these paths.
expect docs-only "docs-only: root markdown" $'CONTRIBUTING.md\nAGENTS.md\n'
expect docs-only "docs-only: docs folder" $'docs/architecture/ADR-001-symbol-based-detection-over-string-matching.md\n'
expect docs-only "docs-only: agent skills and memories" $'.agents/skills/x/SKILL.md\n.serena/memories/a.md\n'
expect docs-only "docs-only: .github top-level markdown" $'.github/copilot-instructions.md\n.github/pull_request_template.md\n'
expect docs-only "docs-only: instructions, prompts, templates" $'.github/instructions/yaml.instructions.md\n.github/prompts/p.prompt.md\n.github/ISSUE_TEMPLATE/01_bug_report.yml\n'
expect docs-only "docs-only: lint and bot config" $'.markdownlint.json\n.yamllint.yml\nCODEOWNERS\n.git-blame-ignore-revs\nrenovate.json\n.vscode/settings.json\n'
expect docs-only "docs-only: no trailing newline" 'docs/crap-metric.md'
expect docs-only "docs-only: blank lines ignored" $'\nCONTRIBUTING.md\n\n'

# docs-tested: a test or the package reads these paths.
expect docs-tested "docs-tested: rule page only" $'docs/rules/Moq1000.md\n'
expect docs-tested "docs-tested: rule index" $'docs/rules/README.md\n'
expect docs-tested "docs-tested: README only" $'README.md\n'
expect docs-tested "docs-tested: rule page plus other docs" $'docs/common-moq-mistakes.md\ndocs/rules/Moq1004.md\n'
expect docs-tested "docs-tested: README plus root markdown" $'CONTRIBUTING.md\nREADME.md\n'
expect docs-tested "docs-tested: tier holds when listed first" $'README.md\ndocs/crap-metric.md\n'
expect docs-tested "docs-tested: rename between docs and rule docs" $'docs/old-name.md\ndocs/rules/Moq1004.md\n'

# full-no-perf: build and test inputs the benchmarks do not load.
expect full-no-perf "no-perf: analyzer test" $'tests/Moq.Analyzers.Test/FooTests.cs\n'
expect full-no-perf "no-perf: nested analyzer test" $'tests/Moq.Analyzers.Test/Common/FooTests.cs\n'
expect full-no-perf "no-perf: Verify snapshot" $'tests/Moq.Analyzers.Test/PackageTests.Baseline#contents.verified.txt\n'
expect full-no-perf "no-perf: other test projects" $'tests/Moq.Analyzers.CSharp13.Test/A.cs\ntests/Moq.Analyzers.CSharp14.Test/B.cs\ntests/Moq.Analyzers.EndToEnd.Test/C.cs\ntests/PerfDiff.Tests/D.cs\n'
expect full-no-perf "no-perf: code fix source" $'src/CodeFixes/FooFixer.cs\n'
expect full-no-perf "no-perf: classifier change" $'build/scripts/ci/classify-changed-paths.sh\n'
expect full-no-perf "no-perf: other workflow" $'.github/workflows/linters.yml\n'
expect full-no-perf "no-perf: release workflow" $'.github/workflows/release.yml\n'
expect full-no-perf "no-perf: markdown nested under .github" $'.github/workflows/notes.md\n'
expect full-no-perf "no-perf: test plus docs plus rule docs" $'docs/rules/Moq1000.md\nREADME.md\ntests/Moq.Analyzers.Test/FooTests.cs\n'

# full: anything that can change what perf measures.
expect full "full: src change" $'src/Analyzers/Foo.cs\n'
expect full "full: common source" $'src/Common/Foo.cs\n'
expect full "full: mixed docs plus source" $'README.md\nCONTRIBUTING.md\nsrc/Analyzers/Foo.cs\n'
expect full "full: source listed first" $'src/Analyzers/Foo.cs\ndocs/crap-metric.md\n'
expect full "full: no-perf then full" $'tests/Moq.Analyzers.Test/FooTests.cs\nsrc/Analyzers/Foo.cs\n'
expect full "full: main workflow" $'.github/workflows/main.yml\n'
expect full "full: composite action" $'.github/actions/setup-restore-build/action.yml\n'
expect full "full: benchmark helpers in test project" $'tests/Moq.Analyzers.Test/Helpers/CompilationHelper.cs\n'
expect full "full: test module initializer" $'tests/Moq.Analyzers.Test/ModuleInitializer.cs\n'
expect full "full: test project file" $'tests/Moq.Analyzers.Test/Moq.Analyzers.Test.csproj\n'
expect full "full: code fix project file" $'src/CodeFixes/Moq.CodeFixes.csproj\n'
expect full "full: benchmarks" $'tests/Moq.Analyzers.Benchmarks/Moq1000SealedClassBenchmarks.cs\n'
expect full "full: perf tool" $'src/tools/PerfDiff/Program.cs\n'
expect full "full: perf scripts" $'build/scripts/perf/PerfCore.ps1\n'
expect full "full: MSBuild props" $'Directory.Build.props\n'
expect full "full: solution file" $'Moq.Analyzers.sln\n'
expect full "full: SDK pin" $'global.json\n'
expect full "full: NuGet config" $'nuget.config\n'
expect full "full: perf entry point" $'Perf.sh\n'
expect full "full: line ending rules" $'.gitattributes\n'
expect full "full: notices packed into nupkg" $'THIRD-PARTY-NOTICES.TXT\n'
expect full "full: markdown under src" $'src/Analyzers/AnalyzerReleases.Unshipped.md\n'
expect full "full: markdown under build" $'build/scripts/perf/README.md\n'
expect full "full: rename from src to docs" $'docs/Foo.md\nsrc/Analyzers/Foo.cs\n'
expect full "full: rename from rule docs to src" $'docs/rules/Moq1000.md\nsrc/Analyzers/Moq1000.md\n'

# Conservative defaults.
expect full "full: empty diff" ''
expect full "full: only blank lines" $'\n\n'
expect full "full: unknown root file" $'new-tool.config\n'
expect full "full: agents dir outside skills" $'.agents/config.json\n'
expect full "full: old skills location" $'.github/skills/x/SKILL.md\n'
expect full "full: git-quoted unusual path" $'"docs/caf\\303\\251.md"\n'

echo "$cases cases, $failures failed"
if [[ "$failures" -ne 0 ]]; then
  exit 1
fi
