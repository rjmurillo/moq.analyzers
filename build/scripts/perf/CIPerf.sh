#!/bin/bash
# Cross-platform CI performance testing script for Linux/macOS
# Equivalent to CIPerf.cmd for Windows

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Execute the PowerShell script with CI parameters and all passed arguments
pwsh -ExecutionPolicy ByPass -NoProfile -File "$SCRIPT_DIR/PerfCore.ps1" -v diag -diff -ci "$@"
exit $?