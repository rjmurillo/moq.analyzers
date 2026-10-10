# Merge Requirements

The `main` branch is protected by a repository ruleset. Classic branch protection is not used.

## Pull request gate

- Every change reaches `main` through a pull request. Direct pushes are blocked.
- A pull request needs one approval from a collaborator with write access.
- The approver must not be the last person who pushed. A bot account with write access, such as `rjmurillo-bot`, counts.
- A new push dismisses earlier approvals.
- All review threads must be resolved.
- All required status checks must pass on a branch that is up to date with `main`.
- CodeQL must report no high or critical security alerts and no errors.
- Squash merge is the only merge method.

## Contributor License Agreement

The `license/cla` check comes from [CLA Assistant](https://cla-assistant.io/rjmurillo/moq.analyzers). Sign the agreement once. The check updates on its own after you sign.

## Pull requests from forks

A maintainer must approve workflow runs for every fork pull request. Maintainers read any change under `.github/workflows` before they approve the run.

## Release tags

Tags that match `v*` cannot be deleted, moved, or force-pushed.

## Admin bypass

Admins can bypass the ruleset only by merging a pull request. Use the bypass for emergencies, not for routine merges.
