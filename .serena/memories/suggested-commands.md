# Suggested Commands

## Build

```bash
dotnet restore
dotnet build /p:PedanticMode=true
```

PedanticMode enables warnings-as-errors and stricter analysis.

## Test

```bash
dotnet test --settings ./build/targets/tests/test.runsettings
```

## Run a Single Test

```bash
dotnet test --settings ./build/targets/tests/test.runsettings --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

## Benchmarks

```bash
dotnet run --project tests/Moq.Analyzers.Benchmarks -c Release
```

## Performance Comparison

```bash
bash Perf.sh
# or on Windows:
Perf.cmd
```

## Lint / Code Analysis

Code analysis runs automatically during build via MSBuild targets in `build/targets/codeanalysis/`.
No separate lint command needed.

## Format

```bash
dotnet format
```

Note: `dotnet format` handles whitespace and style (editorconfig rules). It is distinct from build-time Roslyn analyzer diagnostics, which run during `dotnet build`. Both must pass before committing.

## Package

```bash
dotnet pack src/Analyzers/Moq.Analyzers.csproj -c Release
```

## Git Utilities (Linux)

```bash
git status
git log --oneline -10
git diff
git blame <file>
ls, cd, grep, find
```
