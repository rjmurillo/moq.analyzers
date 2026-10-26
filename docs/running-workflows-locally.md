# Running Workflows Locally with `gh act`

[`gh act`](https://github.com/nektos/gh-act) runs GitHub Actions workflows locally using Docker.
This is useful for verifying CI changes before pushing.

**Install:**

```bash
gh extension install nektos/gh-act
```

**List available jobs:**

```bash
gh act -l                           # list jobs triggered by push
gh act -l pull_request              # list jobs triggered by pull_request
```

**Run a specific job:**

```bash
gh act -j build                     # run the build job
gh act -j analyzer-load-test --matrix tfm:net10.0   # run the Linux leg of the analyzer load test
```

**Run a specific workflow file:**

```bash
gh act -W .github/workflows/main.yml
```

**Filter matrix entries:**

```bash
gh act -j analyzer-load-test --matrix tfm:net10.0
gh act -j analyzer-load-test --matrix build-engine:dotnet
gh act -j build --matrix os:ubuntu-24.04-arm
```

**Enable artifact passing between jobs:**

Jobs that depend on artifacts from earlier jobs (like `analyzer-load-test` downloading
the nupkg from `build`) require a local artifact server:

```bash
gh act -W .github/workflows/main.yml --artifact-server-path /tmp/artifacts
```

**Handle secrets:**

The workflow references optional secrets (`CODACY_PROJECT_TOKEN`, `QLTY_COVERAGE_TOKEN`).
These can be skipped locally, but if needed:

```bash
gh act -s GITHUB_TOKEN="$(gh auth token)"    # pass GitHub token
gh act --secret-file .secrets                # load from file (one KEY=VALUE per line)
```

**Simulate `workflow_dispatch` with inputs:**

```bash
gh act workflow_dispatch --input run_performance=true --input force_baseline=false
```

**Container architecture:**

The project uses ARM runners (`ubuntu-24.04-arm`, `ubuntu-26.04-arm`). Docker defaults to the
host architecture. To force a specific architecture:

```bash
gh act --container-architecture linux/amd64
```

**Create an `.actrc` file** in the repo root to set default flags (one per line):

```text
--artifact-server-path=/tmp/artifacts
--container-architecture=linux/amd64
```

**Skip steps that only make sense in CI:**

`act` sets the `ACT` environment variable automatically. Steps can check for it:

```yaml
if: ${{ !env.ACT }}
```

**Limitations:**

- Windows-based jobs (`windows-latest` for `test`, `windows-2025-vs2026` for the `analyzer-load-test`
  MSBuild legs) do not run in `act`. Test those
  through GitHub Actions directly.
- ARM runner images may not match GitHub-hosted runners exactly.
- The `microsoft/setup-msbuild` action requires Windows and will not work in `act`.
- Composite actions (like `setup-restore-build`) work, but may need the action source
  checked out locally.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `gh act` cannot connect to the Docker daemon | Start Docker, then run `docker info` to check that it answers. On Linux, add your user to the `docker` group or run `gh act` with `sudo`. |
| A job that downloads an artifact fails | Pass `--artifact-server-path /tmp/artifacts` so `act` runs a local artifact server. See "Enable artifact passing between jobs" above. |
| A Windows job or an MSBuild leg does not start | `act` runs Linux containers only. Push the branch and run those jobs in GitHub Actions. |
| A job runs on the wrong architecture | Pass `--container-architecture` with the architecture you need. See "Container architecture" above. |

For full documentation, see [nektosact.com](https://nektosact.com/usage/).
