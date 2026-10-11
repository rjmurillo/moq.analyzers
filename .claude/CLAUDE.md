@../AGENTS.md

## Claude Code

The import above loads the shared repo instructions. This file counts as a
`CLAUDE.md`, so without the import Claude Code would stop reading `AGENTS.md`.

### Skills link

The repo skills live in `.agents/skills/`. Claude Code reads project skills
only from `.claude/skills/`, so a local link bridges the two.

Before you use skills, check that `.claude/skills` is a link to
`../.agents/skills` (`test -L .claude/skills` in bash). If it is a real
folder, do not replace it. Tell the user, because a copy drifts from
`.agents/skills`. If it is missing, run this from the repo root:

```bash
ln -s ../.agents/skills .claude/skills
```

On Windows, run this in `cmd` from the repo root. It needs Developer Mode or
an Administrator prompt:

```bat
mklink /D .claude\skills ..\.agents\skills
```

In Git Bash, `ln -s` copies the folder unless `MSYS=winsymlinks:nativestrict`
is set. Use `mklink` instead.

After you create the link, ask the user to run `/reload-skills`, or start a
new session. Claude Code does not watch a skills folder that did not exist at
startup. In that same session, each later skill edit needs another
`/reload-skills`, or a new session.

The link is ignored by git. Do not commit it. Add or edit skills only in
`.agents/skills/`. The link is not a second home for skills.
