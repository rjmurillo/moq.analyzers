#!/usr/bin/env bash
# Decide how much of the main.yml pipeline a pull request needs.
#
# Input: changed paths on stdin, one per line, relative to the repo root.
# Output: one tier name. Each tier runs everything the tier before it runs.
#
#   docs-only     Skip build, test, load tests, and perf.
#   docs-tested   Build and pack, then run only the tests that read docs.
#                 Skip the full test matrix, load tests, and perf.
#   full-no-perf  Run everything except perf.
#   full          Run everything.
#
# Each path maps to one tier. The pull request gets the highest tier of
# any path. Every path not named below maps to full, so a new file type
# runs everything until someone classifies it here on purpose. An empty
# list also maps to full.
#
# Before moving a path to a lower tier, confirm no skipped step reads it.
# Known traps:
#   docs/rules/*.md  DiagnosticDescriptorMetadataTests reads these files.
#   README.md        Moq.Analyzers.csproj packs it. PackageTests checks it.
#   Benchmarks       Moq.Analyzers.Benchmarks references src/Analyzers and
#                    the helpers in tests/Moq.Analyzers.Test.

set -euo pipefail

readonly TIERS=(docs-only docs-tested full-no-perf full)

# Usage: tier_rank <tier>. Prints the tier's position in TIERS.
tier_rank() {
  local i
  for i in "${!TIERS[@]}"; do
    if [[ "${TIERS[$i]}" == "$1" ]]; then
      echo "$i"
      return 0
    fi
  done

  echo "Unknown tier: $1" >&2
  return 1
}

is_docs_only_markdown() {
  local path="$1"

  if [[ "$path" != *.md || "$path" == "README.md" ]]; then
    return 1
  fi

  # Only the repo root and the top of .github. Deeper markdown can sit
  # next to code, such as src/Analyzers/AnalyzerReleases.Unshipped.md.
  if [[ "$path" != */* ]]; then
    return 0
  fi

  [[ "${path%/*}" == ".github" && "${path#.github/}" != */* ]]
}

is_docs_only() {
  local path="$1"

  case "$path" in
    docs/rules/*)
      return 1
      ;;
    docs/* | .github/skills/* | .github/instructions/* | .github/prompts/* | .github/ISSUE_TEMPLATE/*)
      return 0
      ;;
    .serena/* | .vscode/*)
      return 0
      ;;
    .markdownlint.json | .yamllint.yml | CODEOWNERS | .git-blame-ignore-revs | renovate.json)
      return 0
      ;;
  esac

  is_docs_only_markdown "$path"
}

is_docs_tested() {
  case "$1" in
    docs/rules/* | README.md)
      return 0
      ;;
  esac

  return 1
}

# Paths that cannot change what the perf job measures. The benchmarks
# load only src/Analyzers (with src/Common) and the test helpers.
is_perf_neutral() {
  local path="$1"

  case "$path" in
    tests/Moq.Analyzers.Test/Helpers/* | tests/Moq.Analyzers.Test/ModuleInitializer.cs)
      return 1
      ;;
    tests/Moq.Analyzers.Test/*.cs | tests/Moq.Analyzers.Test/*.verified.*)
      return 0
      ;;
    tests/Moq.Analyzers.CSharp13.Test/* | tests/Moq.Analyzers.CSharp14.Test/*)
      return 0
      ;;
    tests/Moq.Analyzers.EndToEnd.Test/* | tests/PerfDiff.Tests/*)
      return 0
      ;;
    src/CodeFixes/*.cs | build/scripts/ci/*)
      return 0
      ;;
    .github/workflows/main.yml)
      return 1
      ;;
    .github/workflows/*)
      return 0
      ;;
  esac

  return 1
}

path_tier() {
  local path="$1"

  if is_docs_only "$path"; then
    echo docs-only
  elif is_docs_tested "$path"; then
    echo docs-tested
  elif is_perf_neutral "$path"; then
    echo full-no-perf
  else
    echo full
  fi
}

main() {
  local path
  local tier
  local rank
  local max_rank=-1

  # Read all input, even at the top tier. Exiting early would break the
  # upstream pipe and fail the caller under pipefail.
  while IFS= read -r path || [[ -n "$path" ]]; do
    if [[ -z "$path" ]]; then
      continue
    fi

    tier="$(path_tier "$path")"
    rank="$(tier_rank "$tier")"
    if ((rank > max_rank)); then
      max_rank="$rank"
    fi
  done

  if ((max_rank < 0)); then
    max_rank="$(tier_rank full)"
  fi

  echo "${TIERS[$max_rank]}"
}

main
