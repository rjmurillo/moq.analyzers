# Coverage gotcha: src/Common is a shared project (Common.projitems)

## The fact

`src/Common/Common.projitems` compiles `$(MSBuildThisFileDirectory)/**/*.cs`
(SemanticModelExtensions, MoqKnownSymbols, DiagnosticEditProperties, all of
`Moq.Analyzers.Common`) directly into **every** project that imports it:

- `Moq.Analyzers.dll` (shipping)
- `Moq.CodeFixes.dll` (shipping)
- `Moq.Analyzers.Test.dll` (test project imports it at
  `tests/Moq.Analyzers.Test/Moq.Analyzers.Test.csproj:31`)

Each assembly gets its **own compiled copy** of the Common code.

## Why it bites coverage (the 5+ minute trap)

`build/targets/tests/test.runsettings` uses the **Microsoft Code Coverage**
collector and only instruments modules matching `.*Moq\.Analyzers\.dll$` and
`.*Moq\.CodeFixes\.dll$`, with `IncludeTestAssembly=False`.

A direct unit test like `SemanticModelExtensionsTests` calls
`model.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(...)`. That
call binds to the copy compiled into **Moq.Analyzers.Test.dll**, which is NOT
instrumented. Result: the method shows **0 hits / 0% block coverage** in the
cobertura report even though the test passes and clearly executes it.

So: **direct unit tests of `src/Common` code do NOT move the shipping-binary
coverage number.** They exercise a parallel, uninstrumented embedded copy.

## What actually produces shipping coverage for Common code

Only tests that load the real `Moq.Analyzers.dll` / `Moq.CodeFixes.dll` copy:
the analyzer/code-fix verifier tests (`*.Verify*Async` through
`Microsoft.CodeAnalysis.Testing`). To cover a guard inside a Common helper in
the shipping binary, drive input through the analyzer/fixer that calls it, not
through a direct unit test of the helper.

## Tooling note

Do NOT pass `--collect:"XPlat Code Coverage"` (coverlet). The runsettings
already declares the Microsoft `Code Coverage` collector; adding `--collect`
either errors ("Unable to find a datacollector with friendly name 'XPlat Code
Coverage'") or produces a second, misleading report. Just run:
`dotnet test <proj> --settings ./build/targets/tests/test.runsettings` and read
the emitted `*.cobertura.xml`.

## Interaction with defensive guards

Some Common guards defend against malformed/incomplete syntax trees that only
occur in the IDE while typing (e.g. `mock.Setup()` with zero arguments is
`CS1501` and cannot appear in valid compilable code). Such branches are:

1. unreachable by valid-code analyzer tests, and
2. not counted by direct unit tests (shared-project copy, above).
   For the 100% block-coverage bar, treat these as documented defensive
   exclusions (comment + coverage-exclude) rather than trying to fake coverage
   with compiler-error input, which the test contract
   (`.github/instructions/csharp.instructions.md`) forbids.
