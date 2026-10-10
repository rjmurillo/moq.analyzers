#!/usr/bin/env bash
# Tests for classify-changed-paths.sh. Exits non-zero when any case fails.
# Run: bash build/scripts/ci/classify-changed-paths.test.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR
readonly CLASSIFIER="$SCRIPT_DIR/classify-changed-paths.sh"

failures=0
cases=0

# Usage: expect <true|false> <case name> <stdin text>
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

# Docs-only changes skip the expensive steps.
expect false "docs-only: root markdown" $'CONTRIBUTING.md\nAGENTS.md\n'
expect false "docs-only: docs folder" $'docs/architecture/ADR-001-symbol-based-detection-over-string-matching.md\n'
expect false "docs-only: agent skills and memories" $'.github/skills/x/SKILL.md\n.serena/memories/a.md\n'
expect false "docs-only: .github top-level markdown" $'.github/copilot-instructions.md\n.github/pull_request_template.md\n'
expect false "docs-only: instructions, prompts, templates" $'.github/instructions/yaml.instructions.md\n.github/prompts/p.prompt.md\n.github/ISSUE_TEMPLATE/01_bug_report.yml\n'
expect false "docs-only: lint and bot config" $'.markdownlint.json\n.yamllint.yml\nCODEOWNERS\n.git-blame-ignore-revs\nrenovate.json\n.vscode/settings.json\n'
expect false "docs-only: no trailing newline" 'docs/crap-metric.md'
expect false "docs-only: blank lines ignored" $'\nCONTRIBUTING.md\n\n'

# Mixed changes run everything.
expect true "mixed: docs plus source" $'README.md\nCONTRIBUTING.md\nsrc/Analyzers/Foo.cs\n'
expect true "mixed: source listed first" $'src/Analyzers/Foo.cs\ndocs/crap-metric.md\n'

# CI definitions run everything.
expect true "workflow change" $'.github/workflows/main.yml\n'
expect true "composite action change" $'.github/actions/setup-restore-build/action.yml\n'
expect true "classifier change" $'build/scripts/ci/classify-changed-paths.sh\n'

# Source, tests, and build inputs run everything.
expect true "src change" $'src/Analyzers/Foo.cs\n'
expect true "tests change" $'tests/Moq.Analyzers.Test/FooTests.cs\n'
expect true "MSBuild props" $'Directory.Build.props\n'
expect true "solution file" $'Moq.Analyzers.sln\n'
expect true "SDK pin" $'global.json\n'
expect true "NuGet config" $'nuget.config\n'
expect true "perf entry point" $'Perf.sh\n'
expect true "line ending rules" $'.gitattributes\n'

# Files that look like docs but feed the build.
expect true "rule docs read by tests" $'docs/rules/Moq1000.md\n'
expect true "README packed into nupkg" $'README.md\n'
expect true "notices packed into nupkg" $'THIRD-PARTY-NOTICES.TXT\n'
expect true "markdown under src" $'src/Analyzers/AnalyzerReleases.Unshipped.md\n'
expect true "markdown nested under .github" $'.github/workflows/notes.md\n'
expect true "markdown under build" $'build/scripts/perf/README.md\n'

# Conservative defaults.
expect true "empty diff" ''
expect true "unknown root file" $'new-tool.config\n'
expect true "git-quoted unusual path" $'"docs/caf\\303\\251.md"\n'

echo "$cases cases, $failures failed"
if [[ "$failures" -ne 0 ]]; then
  exit 1
fi
