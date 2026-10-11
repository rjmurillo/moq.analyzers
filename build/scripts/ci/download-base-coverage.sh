#!/usr/bin/env bash
# Download the coverage report that main CI measured for a PR's base commit.
#
# Docs-only and docs-tested pull requests skip tests, so they reuse the
# coverage from the main.yml push run for their exact base commit. If that
# run is still queued or running, wait for it. Never use another commit's
# coverage: only the base commit's own run measured this PR's code.
#
# Input (environment):
#   GH_REPO                owner/name of the repository
#   BASE_SHA               base commit of the pull request
#   ARTIFACT_NAME          name of the coverage artifact to download
#   DOWNLOAD_DIR           directory to download the artifact into
#   GITHUB_OUTPUT          file that receives "report=<path>"
#   WAIT_TIMEOUT_SECONDS   longest time to wait for an unfinished run
#   POLL_INTERVAL_SECONDS  time between checks of an unfinished run
# Requires gh with a token that can read actions.

set -euo pipefail

# Set run_id to the main.yml push run to use for BASE_SHA, or fail.
find_run() {
  local runs_api="repos/$GH_REPO/actions/workflows/main.yml/runs?event=push&branch=main&head_sha=$BASE_SHA&per_page=1"

  run_id="$(gh api "$runs_api&status=success" --jq '.workflow_runs[0].id // empty')"
  if [[ -n "$run_id" ]]; then
    return 0
  fi

  # The newest run for this exact commit. It may still be running, or it
  # may have finished since the query above.
  run_id="$(gh api "$runs_api" --jq '.workflow_runs[0].id // empty')"
  if [[ -z "$run_id" ]]; then
    echo "::error::No main.yml push run exists for base $BASE_SHA. Run main CI on that commit, then re-run this job."
    return 1
  fi

  wait_for_success "$run_id"
}

# Wait until the run finishes. Fail unless it succeeded in time.
wait_for_success() {
  local run_id="$1"
  local deadline=$((SECONDS + WAIT_TIMEOUT_SECONDS))
  local state status conclusion

  while true; do
    if ! state="$(gh api "repos/$GH_REPO/actions/runs/$run_id" --jq '"\(.status) \(.conclusion // "none")"')"; then
      echo "::warning::Could not read the state of run $run_id. Trying again."
      state="unknown none"
    fi
    read -r status conclusion <<< "$state"
    if [[ "$status" == "completed" ]]; then
      break
    fi
    if (( SECONDS >= deadline )); then
      echo "::error::Main CI run $run_id for base $BASE_SHA did not finish within $WAIT_TIMEOUT_SECONDS seconds. Re-run this job after it passes."
      return 1
    fi
    echo "Main CI run $run_id for base $BASE_SHA is $status. Checking again in $POLL_INTERVAL_SECONDS seconds."
    sleep "$POLL_INTERVAL_SECONDS"
  done

  if [[ "$conclusion" != "success" ]]; then
    echo "::error::Main CI run $run_id for base $BASE_SHA finished as '$conclusion'. Fix main CI on that commit, then re-run this job."
    return 1
  fi
}

main() {
  local run_id run_sha report

  find_run

  run_sha="$(gh api "repos/$GH_REPO/actions/runs/$run_id" --jq '.head_sha')"
  echo "Using coverage from run $run_id (main at $run_sha)"
  if ! gh run download "$run_id" --name "$ARTIFACT_NAME" --dir "$DOWNLOAD_DIR"; then
    echo "::error::Could not download '$ARTIFACT_NAME' from run $run_id. It may have expired. Re-run main.yml on main, then re-run this job."
    exit 1
  fi

  report="$DOWNLOAD_DIR/Cobertura.xml"
  if [[ ! -s "$report" ]]; then
    echo "::error::'$ARTIFACT_NAME' from run $run_id has no Cobertura.xml."
    exit 1
  fi
  echo "report=$report" >> "$GITHUB_OUTPUT"
}

main "$@"
