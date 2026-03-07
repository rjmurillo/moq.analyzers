# Release Branch Patterns

**Last Updated**: 2026-03-06
**Sessions Analyzed**: 1

## Constraints (HIGH confidence)

- nbgv 3-part versions (e.g. `0.4.2`) place the git height in the 4th component (revision), which NuGet semVer 2 strips. Use the `{height}` placeholder in the prerelease tag for unique NuGet versions: `"version": "0.4.2-alpha.{height}"` produces `0.4.2-alpha.1`, `0.4.2-alpha.2`, etc. Without `{height}`, all commits produce the same package version `0.4.2-alpha`. (Session 1, 2026-03-06)

## Notes for Review (LOW confidence)

- `release/v0.4.1` branch history is the reference for release branch patterns (version.json format, cherry-pick workflow). Check commit 3d66e79 for the height placeholder pattern. (Session 1, 2026-03-06)
- Cherry-pick workflow: develop and test on a branch off main, raise PR, merge to main, then cherry-pick the merge commit back to the release branch. This keeps main as the source of truth. (Session 1, 2026-03-06)
