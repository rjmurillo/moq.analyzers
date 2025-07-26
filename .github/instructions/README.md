# Instruction File Index

This table maps file patterns in the repository to their corresponding instruction files. Use this as a quick reference to ensure you are following the correct guidance for each file type. If a file type is not listed, refer to `generic.instructions.md`.

| File Pattern            | Instruction File                                   | Description/Notes                       |
|------------------------|----------------------------------------------------|-----------------------------------------|
| `*.cs`                 | `csharp.instructions.md`                           | C# source files                         |
| `*.csproj`, `*.sln`    | `project.instructions.md`                          | Project/solution files                  |
| `*.md`                 | `markdown.instructions.md`                         | Markdown documentation                  |
| `*.json`               | `json.instructions.md`                             | JSON config                             |
| `*.yml`, `*.yaml`      | `yaml.instructions.md`                             | CI/CD workflows                         |
| `*.sh`, `*.ps1`        | `shell.instructions.md`                            | Shell/PowerShell scripts                |
| `*.xml`                | `xml.instructions.md`                              | XML config/docs                         |
| `*.txt`                | `text.instructions.md`                             | Text files                              |
| `.editorconfig`        | `editorconfig.instructions.md`                     | EditorConfig rules                      |
| `.gitignore`           | `gitignore.instructions.md`                        | Git ignore rules                        |
| `*.props`, `*.targets` | `msbuild.instructions.md`                          | MSBuild property/target files           |
| _Other_                | `generic.instructions.md`                          | Fallback for unknown file types         |

**Note:** If you are editing a file type not listed above, always check for a matching instruction file in this directory or use the generic fallback. When in doubt, escalate by tagging `@repo-maintainers` in your PR. 