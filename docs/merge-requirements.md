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

## Merge queue

Pull requests merge through a merge queue. The branch does not need to be up to date with `main`.

- When the pull request gate passes, select **Merge when ready**. This adds the pull request to the queue.
- The queue builds a temporary branch named `gh-readonly-queue/main/...`. It holds `main` plus the queued pull requests.
- Every required check runs again on that branch. The `merge_group` event triggers the GitHub Actions checks.
- The queue squash merges a group only when all its required checks pass.
- A failed check removes the pull request from the queue. Fix it, then queue it again.
- A merge group runs the full pipeline. The docs-only skip applies to pull requests only.
- `Validate PR title` passes on a merge group without reading a title. The title was checked on the pull request.
- Codacy and CLA Assistant report their checks on merge groups on their own.
- The CodeQL code scanning rule applies to pull requests, not to merge groups.

## Contributor License Agreement

The `license/cla` check comes from [CLA Assistant](https://cla-assistant.io/rjmurillo/moq.analyzers). Sign the agreement once. The check updates on its own after you sign.

## Pull requests from forks

A maintainer must approve workflow runs for every fork pull request. Maintainers read any change under `.github/workflows` before they approve the run.

## Release tags

Tags that match `v*` cannot be deleted, moved, or force-pushed.

## Admin bypass

Admins can bypass the ruleset only by merging a pull request. Use the bypass for emergencies, not for routine merges.
