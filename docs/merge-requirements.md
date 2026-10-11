# Merge requirements

A repository ruleset protects the `main` branch. The repository does not use classic branch protection.

## Pull request gate

- Every change reaches `main` through a pull request. Direct pushes are blocked.
- A pull request needs one approval from a collaborator with write access.
- The approver must not be the last person who pushed. A bot account with write access, such as `rjmurillo-bot`, counts.
- A new push dismisses earlier approvals.
- All review threads must be resolved.
- All required status checks must pass on the pull request.
- CodeQL must report no high or critical security alerts and no errors.
- Squash merge is the only merge method.

## Branch freshness

A pull request does not need to be up to date with `main` before it merges. Required checks run on the pull request merge commit at the time of each push.

GitHub merge queues are not available for repositories owned by a personal account. The workflows that report required checks still trigger on `merge_group`, so a queue can be added if the repository moves to an organization.

## Contributor License Agreement

The `license/cla` check comes from [CLA Assistant](https://cla-assistant.io/rjmurillo/moq.analyzers). Sign the agreement once. The check updates on its own after you sign.

## Pull requests from forks

A maintainer must approve workflow runs for every fork pull request. Maintainers read any change under `.github/workflows` before they approve the run.

## Release tags

Tags that match `v*` cannot be deleted, moved, or force-pushed.

## Admin bypass

Admins can bypass the ruleset only by merging a pull request. Use the bypass for emergencies, not for routine merges.
