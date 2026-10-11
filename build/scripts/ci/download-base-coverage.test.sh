#!/usr/bin/env bash
# Tests for download-base-coverage.sh. Exits non-zero when any case fails.
# Run: bash build/scripts/ci/download-base-coverage.test.sh
#
# A fake gh on PATH plays the GitHub API. FAKE_SCENARIO picks its answers.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR
readonly SUBJECT="$SCRIPT_DIR/download-base-coverage.sh"

WORK_DIR="$(mktemp -d)"
readonly WORK_DIR
trap 'rm -r -- "$WORK_DIR"' EXIT

mkdir "$WORK_DIR/bin"
cat > "$WORK_DIR/bin/gh" <<'FAKE'
#!/usr/bin/env bash
# Fake gh. Answers "api <url> --jq <expr>" and "run download".
set -euo pipefail

calls=0
if [[ -f "$FAKE_STATE" ]]; then
  calls="$(< "$FAKE_STATE")"
fi

if [[ "$1" == "run" ]]; then
  [[ "$FAKE_SCENARIO" == "download-fails" ]] && exit 1
  dir="${*: -1}"
  mkdir -p "$dir"
  if [[ "$FAKE_SCENARIO" == "empty-report" ]]; then
    : > "$dir/Cobertura.xml"
  else
    echo '<coverage/>' > "$dir/Cobertura.xml"
  fi
  exit 0
fi

url="$2"
jq_expr="$4"
done_ok='{"status":"completed","conclusion":"success","head_sha":"abc"}'
running='{"status":"in_progress","conclusion":null,"head_sha":"abc"}'

case "$url" in
  *status=success*)
    case "$FAKE_SCENARIO" in
      success|download-fails|empty-report) json='{"workflow_runs":[{"id":7}]}' ;;
      *) json='{"workflow_runs":[]}' ;;
    esac ;;
  */workflows/main.yml/runs*)
    case "$FAKE_SCENARIO" in
      no-run) json='{"workflow_runs":[]}' ;;
      # The run finished after the success query, or finished badly.
      finished-between-queries|failure)
        json='{"workflow_runs":[{"id":42,"status":"completed"}]}' ;;
      *) json='{"workflow_runs":[{"id":42,"status":"in_progress"}]}' ;;
    esac ;;
  */actions/runs/7)
    json="$done_ok" ;;
  */actions/runs/42)
    echo $((calls + 1)) > "$FAKE_STATE"
    case "$FAKE_SCENARIO" in
      finished-between-queries) json="$done_ok" ;;
      in-progress)
        json="$done_ok"
        if (( calls < 2 )); then json="$running"; fi ;;
      queued)
        json="$done_ok"
        if (( calls < 1 )); then json='{"status":"queued","conclusion":null}'; fi ;;
      api-error)
        if (( calls == 0 )); then
          echo "HTTP 502" >&2
          exit 1
        fi
        json="$done_ok" ;;
      failure) json='{"status":"completed","conclusion":"failure"}' ;;
      cancelled)
        json='{"status":"completed","conclusion":"cancelled"}'
        if (( calls < 1 )); then json="$running"; fi ;;
      timeout) json="$running" ;;
    esac ;;
  *)
    echo "fake gh: unexpected url $url" >&2
    exit 2 ;;
esac
jq -r "$jq_expr" <<< "$json"
FAKE
chmod +x "$WORK_DIR/bin/gh"

failures=0
cases=0

# Usage: expect <exit code> <run ID in output, or empty> <message> <scenario>
expect() {
  local expected_rc="$1"
  local expected_run="$2"
  local expected_message="$3"
  local scenario="$4"
  local dir="$WORK_DIR/$scenario"
  local rc=0 log

  cases=$((cases + 1))
  mkdir "$dir"
  : > "$dir/output"
  log="$(PATH="$WORK_DIR/bin:$PATH" \
    GH_REPO=owner/repo BASE_SHA=base123 ARTIFACT_NAME=coverage \
    DOWNLOAD_DIR="$dir/download" GITHUB_OUTPUT="$dir/output" \
    WAIT_TIMEOUT_SECONDS=2 POLL_INTERVAL_SECONDS=0 \
    FAKE_SCENARIO="$scenario" FAKE_STATE="$dir/state" \
    bash "$SUBJECT" 2>&1)" || rc=$?

  local problems=()
  [[ "$rc" == "$expected_rc" ]] || problems+=("exit $rc, expected $expected_rc")
  [[ "$log" == *"$expected_message"* ]] || problems+=("no '$expected_message' in output")
  if [[ -n "$expected_run" ]]; then
    [[ "$log" == *"Using coverage from run $expected_run "* ]] || problems+=("did not use run $expected_run")
    [[ "$(< "$dir/output")" == "report=$dir/download/Cobertura.xml" ]] || problems+=("no report output")
  else
    [[ ! -s "$dir/output" ]] || problems+=("wrote a report output")
  fi

  if (( ${#problems[@]} == 0 )); then
    echo "[PASS] $scenario"
    return 0
  fi

  echo "[FAIL] $scenario: ${problems[*]}"
  printf '%s\n' "$log" | sed 's/^/    /'
  failures=$((failures + 1))
}

# Use a successful run for the exact base commit.
expect 0 7 "Using coverage" success
expect 0 42 "Using coverage" finished-between-queries
expect 0 42 "is in_progress. Checking again" in-progress
expect 0 42 "is queued. Checking again" queued
expect 0 42 "::warning::Could not read the state of run 42" api-error

# Fail with a clear error, and never use another commit's coverage.
expect 1 "" "::error::No main.yml push run exists for base base123" no-run
expect 1 "" "::error::Main CI run 42 for base base123 finished as 'failure'" failure
expect 1 "" "::error::Main CI run 42 for base base123 finished as 'cancelled'" cancelled
expect 1 "" "::error::Main CI run 42 for base base123 did not finish within 2 seconds" timeout
expect 1 "" "::error::Could not download 'coverage' from run 7" download-fails
expect 1 "" "::error::'coverage' from run 7 has no Cobertura.xml" empty-report

echo "$((cases - failures))/$cases passed"
(( failures == 0 ))
