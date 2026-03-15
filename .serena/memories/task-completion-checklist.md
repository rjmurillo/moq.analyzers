# Task Completion Checklist

When a coding task is completed, verify the following before committing:

## 1. Build

```bash
dotnet build /p:PedanticMode=true
```

Must compile with zero warnings and zero errors.

## 2. Test

```bash
dotnet test --settings ./build/targets/tests/test.runsettings
```

All tests must pass. New functionality requires new tests.

## 3. Code Quality

- No cyclomatic complexity > 10
- No methods > 60 lines
- No nested code
- SOLID, DRY, YAGNI principles followed
- Symbol-based detection, never string matching

## 4. Analyzer-Specific

- If adding/modifying a diagnostic: update `AnalyzerReleases.Unshipped.md`
- If adding a new rule: add documentation in `docs/rules/`
- If modifying public API: check backward compatibility with Roslyn 4.8+

## 5. Format

```bash
dotnet format --verify-no-changes
```

## 6. Commit

- Use Conventional Commits format
- Atomic commits (one logical change per commit)
