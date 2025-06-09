#!/bin/bash
# Cross-platform performance testing script for Linux/macOS
# Equivalent to Perf.cmd for Windows

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Execute the PowerShell script with all passed arguments
pwsh -ExecutionPolicy ByPass -NoProfile -command "& \"$SCRIPT_DIR/build/scripts/perf/PerfCore.ps1\" \"$@\""
exit $?