#!/usr/bin/env bash
# Decide whether a pull request needs the full CI pipeline in main.yml.
#
# Input: changed paths on stdin, one per line, relative to the repo root.
# Output: "true" when the full pipeline must run, "false" when it can skip.
#
# The list below names paths that are safe to skip. Every other path runs
# the full pipeline. A new file type therefore runs CI until someone adds it
# here on purpose. An empty list also runs the full pipeline.
#
# Before adding a path, confirm no build, test, pack, or perf step reads it.
# Known traps:
#   docs/rules/*.md  DiagnosticDescriptorMetadataTests reads these files.
#   README.md        Moq.Analyzers.csproj packs it into the nupkg.

set -euo pipefail

is_skippable_markdown() {
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

is_skippable() {
  local path="$1"

  case "$path" in
    docs/rules/*)
      return 1
      ;;
    docs/* | .agents/skills/* | .github/instructions/* | .github/prompts/* | .github/ISSUE_TEMPLATE/*)
      return 0
      ;;
    .serena/* | .vscode/*)
      return 0
      ;;
    .markdownlint.json | .yamllint.yml | CODEOWNERS | .git-blame-ignore-revs | renovate.json)
      return 0
      ;;
  esac

  is_skippable_markdown "$path"
}

main() {
  local path
  local saw_path=false
  local needs_full_run=false

  # Read all input, even after a match. Exiting early would break the
  # upstream pipe and fail the caller under pipefail.
  while IFS= read -r path || [[ -n "$path" ]]; do
    if [[ -z "$path" ]]; then
      continue
    fi

    saw_path=true
    if ! is_skippable "$path"; then
      needs_full_run=true
    fi
  done

  if [[ "$saw_path" == false ]]; then
    needs_full_run=true
  fi

  echo "$needs_full_run"
}

main
