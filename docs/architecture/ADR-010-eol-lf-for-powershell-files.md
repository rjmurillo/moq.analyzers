---
title: "ADR-010: Use eol=lf for PowerShell Files in .gitattributes"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "git", "powershell", "line-endings"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

The repository uses PowerShell scripts in Git hooks and build automation (e.g., `build/scripts/todo-scanner/Scan-TodoComments.ps1`). Issue #1081 reported that the pre-push hook fails with a PowerShell parse error. The root cause: CRLF line endings cause `<# ... #>` block comment terminators to include a trailing `\r`, which PowerShell cannot parse when invoked from Git Bash or Unix shells.

The `.gitattributes` file already enforces `eol=lf` for `.githooks/**`. However, PowerShell scripts under `build/scripts/` were not covered by any explicit rule. They inherited `text=auto`, which produces CRLF on Windows checkouts. All pushes were blocked unless contributors bypassed the hook with `--no-verify`.

PowerShell 7+ reads both CRLF and LF correctly on all platforms (PowerShell/PowerShell Discussion #16569). The only known scenario where LF causes problems is Authenticode-signed scripts (PowerShell/PowerShell#3361, PowerShell/PowerShell#25246). This repository does not use Authenticode signing.

This repository targets PowerShell 7+ (pwsh) for all script execution. Windows PowerShell 5.1 is not a supported runtime for repository scripts.

## Decision

Set `*.ps1 text eol=lf`, `*.psm1 text eol=lf`, and `*.psd1 text eol=lf` in `.gitattributes`. This applies globally to all PowerShell files in the repository, regardless of directory.

The global rule was chosen over path-specific rules (e.g., `build/scripts/**`) because path-specific rules are fragile. New PowerShell files added in other directories would silently inherit `text=auto` and could reintroduce the same bug.

## Consequences

### Positive

- **POS-001**: PowerShell scripts parse correctly on Unix, macOS, and Windows. Block comment terminators no longer include a trailing `\r`.
- **POS-002**: Pre-push hooks and build scripts execute without parse errors from Git Bash on Windows.
- **POS-003**: Consistent with the existing `.githooks/** text eol=lf` precedent in this repository.
- **POS-004**: New PowerShell files added anywhere in the repo automatically inherit the correct line ending. No per-directory maintenance required.

### Negative

- **NEG-001**: If Authenticode script signing is ever required, this decision must be revisited. LF line endings break signature verification on Windows PowerShell 5.1.
- **NEG-002**: Contributors must run `git add --renormalize . && git checkout .` after pulling the fix to update their working tree. Without this step, locally cached CRLF copies persist until the file is next checked out.
- **NEG-003**: Deviates from the conventional guidance that recommends CRLF for PowerShell files. Contributors familiar with that convention may question this choice.

## Alternatives Considered

### eol=crlf (Conventional Windows Default)

- **ALT-001**: Set `*.ps1 text eol=crlf` to match conventional gitattributes guidance for PowerShell files. PowerShell is historically Windows-native, and most template repositories use CRLF. **Rejected**: CRLF causes parse failures when scripts are invoked from Git Bash or Unix shells. The `\r` appended to `#>` breaks block comment parsing. This is the exact bug reported in issue #1081.

### Path-Specific Rules Only

- **ALT-002**: Add `build/scripts/** text eol=lf` to target only the known problematic directory, leaving other PowerShell files at `text=auto`. **Rejected**: Fragile. New PowerShell files in other directories would silently inherit `text=auto` and could reintroduce the bug. Requires ongoing maintenance as the directory structure evolves.

### Do Nothing

- **ALT-003**: Leave `.gitattributes` unchanged and document the workaround (use `--no-verify` or manually convert line endings). **Rejected**: Blocks all contributors from pushing without a workaround. Undermines the purpose of Git hooks. Does not fix the root cause.

## Implementation Notes

- **IMP-001**: Add the three glob rules (`*.ps1`, `*.psm1`, `*.psd1`) to `.gitattributes` in the file-type section alongside existing extension-based rules.
- **IMP-001a**: Add a corresponding `[*.{ps1,psm1,psd1}]` section to `.editorconfig` with `end_of_line = lf` so editors enforce LF on save, preventing CRLF from being introduced during editing.
- **IMP-002**: After pulling the merged fix, contributors should run `git add --renormalize . && git checkout .` to apply the new line ending rules. Alternatively, a fresh clone applies the rules automatically. Document this in the PR description and CONTRIBUTING.md.
- **IMP-003**: Verify the fix by running the pre-push hook on Windows (Git Bash), macOS, and Linux. The `Scan-TodoComments.ps1` script must parse without errors on all three platforms.

## References

- **REF-001**: GitHub Issue #1081 -- Pre-push hook fails with PowerShell parse error in `Scan-TodoComments.ps1`
- **REF-002**: PowerShell/PowerShell Discussion #16569 -- PowerShell 7+ handles LF on all platforms
- **REF-003**: PowerShell/PowerShell#3361 -- LF breaks Authenticode signature verification
- **REF-004**: PowerShell/PowerShell#25246 -- Additional Authenticode/LF interaction
- **REF-005**: Scott Hanselman, "Carriage Returns and Line Feeds Will Ultimately Bite You" -- cross-platform eol guidance
- **REF-006**: Existing `.gitattributes` rule: `.githooks/** text eol=lf`
