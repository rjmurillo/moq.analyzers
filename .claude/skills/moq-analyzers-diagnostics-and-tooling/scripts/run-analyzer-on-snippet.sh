#!/usr/bin/env bash
# run-analyzer-on-snippet.sh — run the BUILT Moq.Analyzers DLLs against an arbitrary C# snippet.
#
# Usage:
#   ./run-analyzer-on-snippet.sh <snippet.cs> [moq-version]
#
#   <snippet.cs>   Path to a self-contained C# file (must include its own `using Moq;` etc.).
#   [moq-version]  Optional Moq NuGet version. Default: 4.18.4 (the "new Moq" the test suite pins).
#                  Use 4.8.2 to reproduce the "old Moq" axis of the test matrix.
#
# Environment overrides:
#   MOQ_ANALYZERS_CONFIG   debug (default) or release — which artifacts/bin/Moq.Analyzers/<config>/ to load.
#   SNIPPET_TFM            Target framework of the harness project. Default: net8.0.
#   SNIPPET_LANGVERSION    C# language version for the snippet. Default: latest (the host SDK's newest
#                          stable C#, so modern-host repros such as C# 13 `params` collections parse).
#                          Set to 12 to mirror the pinned Roslyn 4.8 test compiler; use preview for
#                          preview-only syntax.
#   KEEP_HARNESS=1         Keep the temp harness project directory and print its path (for debugging).
#
# What it does:
#   1. Locates the repo root from this script's location (works from any cwd).
#   2. Verifies the analyzer DLLs exist (tells you to `dotnet build` if not).
#   3. Generates a throwaway net8.0 class-library project OUTSIDE the repo tree
#      (so the repo's Directory.Build.props / CPM / PedanticMode do not interfere), with:
#        - <PackageReference Include="Moq" Version="..."/> (a REAL Moq reference, so
#          IsMockReferenced() early-exits do not silently disable every analyzer), and
#        - the same three DLLs the shipped nupkg puts in analyzers/dotnet/cs
#          (Moq.Analyzers.dll, Moq.CodeFixes.dll, Microsoft.CodeAnalysis.AnalyzerUtilities.dll)
#          wired in as <Analyzer> items.
#   4. Writes an .editorconfig that forces every ID found in src/Common/DiagnosticIds.cs to
#      `warning`, so Info/Suggestion-severity rules (e.g. Moq1400) show up in build output.
#   5. Builds and prints every MoqXXXX diagnostic as: file(line,col): severity MoqID: message
#      Compiler (CSxxxx) errors are printed too, so you can tell "no diagnostics" apart from
#      "snippet did not compile". Analyzer crashes (AD0001 — Roslyn's "analyzer threw an
#      exception") are surfaced separately, because they exit the build 0 as a warning and
#      would otherwise be reported as a clean snippet.
#
# Exit codes: 0 = harness ran (with or without Moq diagnostics); 1 = usage/setup error;
#             2 = snippet failed to compile (CS errors printed);
#             3 = an analyzer crashed (AD0001) — the priority-1 failure class this tool exists to catch.

set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
    grep '^#' "$0" | sed -n '2,20p' | sed 's/^# \{0,1\}//'
    exit 1
fi

SNIPPET="$1"
MOQ_VERSION="${2:-4.18.4}"
CONFIG="${MOQ_ANALYZERS_CONFIG:-debug}"
TFM="${SNIPPET_TFM:-net8.0}"
LANGVERSION="${SNIPPET_LANGVERSION:-latest}"

# These three values are interpolated verbatim into the generated Snippet.csproj XML.
# Reject XML-significant characters so a stray/hostile value cannot break the project
# file or inject MSBuild elements (e.g. an <Exec> target that dotnet build would run).
# Denylist rather than allowlist so legitimate NuGet version ranges (e.g. [4.8.2,4.9.0))
# still pass.
for _xmlvar in TFM LANGVERSION MOQ_VERSION; do
    case "${!_xmlvar}" in
        *'<'* | *'>'* | *'&'* | *'"'* | *"'"*)
            echo "error: $_xmlvar contains XML-unsafe characters: ${!_xmlvar}" >&2
            exit 1
            ;;
    esac
done

if [[ ! -f "$SNIPPET" ]]; then
    echo "error: snippet file not found: $SNIPPET" >&2
    exit 1
fi
SNIPPET="$(cd -- "$(dirname -- "$SNIPPET")" && pwd)/$(basename -- "$SNIPPET")"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)"

ANALYZER_DIR="$REPO_ROOT/artifacts/bin/Moq.Analyzers/$CONFIG"
for dll in Moq.Analyzers.dll Moq.CodeFixes.dll Microsoft.CodeAnalysis.AnalyzerUtilities.dll; do
    if [[ ! -f "$ANALYZER_DIR/$dll" ]]; then
        echo "error: $ANALYZER_DIR/$dll not found." >&2
        echo "Build first: cd $REPO_ROOT && dotnet build src/Analyzers/Moq.Analyzers.csproj" >&2
        echo "(or set MOQ_ANALYZERS_CONFIG=release if you built with -c Release)" >&2
        exit 1
    fi
done

# Harness lives OUTSIDE the repo so repo-root Directory.Build.props, Central Package
# Management, and analyzers configured for this repo do not apply to the snippet.
WORK="$(mktemp -d "${TMPDIR:-/tmp}/moq-analyzer-snippet.XXXXXX")"
if [[ "${KEEP_HARNESS:-0}" == "1" ]]; then
    echo "harness project: $WORK" >&2
    trap - EXIT
else
    trap 'rm -rf "$WORK"' EXIT
fi

cp "$SNIPPET" "$WORK/Snippet.cs"

cat > "$WORK/Snippet.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$TFM</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>$LANGVERSION</LangVersion>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <EnableNETAnalyzers>false</EnableNETAnalyzers>
    <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Moq" Version="$MOQ_VERSION" />
  </ItemGroup>
  <ItemGroup>
    <Analyzer Include="$ANALYZER_DIR/Moq.Analyzers.dll" />
    <Analyzer Include="$ANALYZER_DIR/Moq.CodeFixes.dll" />
    <Analyzer Include="$ANALYZER_DIR/Microsoft.CodeAnalysis.AnalyzerUtilities.dll" />
  </ItemGroup>
</Project>
EOF

# Force every Moq rule to at least 'warning' so Info-severity rules appear in build output.
# IDs are scraped from the repo's single source of truth so this never goes stale.
{
    echo "root = true"
    echo ""
    echo "[*.cs]"
    grep -o '"Moq[0-9]\{4\}"' "$REPO_ROOT/src/Common/DiagnosticIds.cs" | tr -d '"' | sort -u \
        | while read -r id; do
              echo "dotnet_diagnostic.${id}.severity = warning"
          done
} > "$WORK/.editorconfig"

BUILD_LOG="$WORK/build.log"
set +e
dotnet build "$WORK/Snippet.csproj" -nologo -v quiet -consoleLoggerParameters:NoSummary \
    > "$BUILD_LOG" 2>&1
BUILD_EXIT=$?
set -e

# Print Moq diagnostics (deduplicated: MSBuild echoes each once per target invocation).
MOQ_LINES="$(grep -E 'Moq[0-9]{4}' "$BUILD_LOG" | sed 's/ \[.*Snippet\.csproj\]$//' | sort -u || true)"
CS_ERRORS="$(grep -E 'error CS[0-9]+' "$BUILD_LOG" | sed 's/ \[.*Snippet\.csproj\]$//' | sort -u || true)"
# AD0001 = Roslyn's "analyzer threw an exception". It surfaces as a *warning*, so with
# TreatWarningsAsErrors=false the build exits 0 and it is neither a MoqXXXX line nor a CS
# error — the harness would otherwise report a crashing analyzer as a clean snippet.
AD_CRASHES="$(grep -E 'AD0001' "$BUILD_LOG" | sed 's/ \[.*Snippet\.csproj\]$//' | sort -u || true)"

if [[ -n "$CS_ERRORS" ]]; then
    echo "--- snippet failed to compile (fix these first; analyzers may be unreliable on broken code) ---"
    echo "$CS_ERRORS"
fi

if [[ -n "$AD_CRASHES" ]]; then
    echo "--- ANALYZER CRASH (AD0001) — priority-1 failure; an analyzer threw an exception ---"
    echo "$AD_CRASHES"
fi

echo "--- Moq.Analyzers diagnostics (config=$CONFIG, Moq $MOQ_VERSION, $TFM, C# $LANGVERSION) ---"
if [[ -n "$MOQ_LINES" ]]; then
    echo "$MOQ_LINES"
else
    echo "(none)"
fi

# An analyzer crash outranks every other outcome: it is the failure class this tool exists to
# catch, and Roslyn disables the crashing analyzer for the rest of the session.
if [[ -n "$AD_CRASHES" ]]; then
    exit 3
fi
if [[ -n "$CS_ERRORS" ]]; then
    exit 2
fi
if [[ $BUILD_EXIT -ne 0 && -z "$MOQ_LINES" ]]; then
    echo "--- build failed for a non-snippet reason; full log follows ---" >&2
    cat "$BUILD_LOG" >&2
    exit 1
fi
exit 0
