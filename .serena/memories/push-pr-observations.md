# Skill Sidecar Learnings: push-pr

**Last Updated**: 2026-03-06
**Sessions Analyzed**: 1

## Constraints (HIGH confidence)

- When PR includes workflow file changes (.github/workflows/**), validate locally with actionlint first, then test with `gh act -n` (dry-run) before pushing. (Session 1, 2026-03-06)

## Preferences (MED confidence)

- Run actionlint before gh act to catch YAML errors faster. Actionlint needs no Docker; gh act requires Docker and sudo. (Session 1, 2026-03-06)

## Edge Cases (MED confidence)

- `gh act` may require elevated permissions for Docker socket access. If `sudo gh act` fails because the extension is not in the sudo PATH, locate the binary with `which gh-act` or check the gh extensions directory, then invoke it directly with sudo. (Session 1, 2026-03-06)
- Unsupported runners (e.g., `ubuntu-24.04-arm`) need platform mappings: `-P ubuntu-24.04-arm=catthehacker/ubuntu:act-latest`. Map all matrix runners similarly. (Session 1, 2026-03-06)
- Actionlint runs via Docker: `sudo docker run --rm -v "$(pwd)":/repo -w /repo rhysd/actionlint:latest -color`. (Session 1, 2026-03-06)
