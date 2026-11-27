# Decision: A Windows "Stack overflow" test-host crash was commit-limit exhaustion

Date measured: 2026-10-26. Recorded: 2026-11-25. Source PR: #1489, merged as 53b196d.

## Question

After the Roslyn 5.0 upgrade (#1484), the Windows `test` job crashed with `Test host process crashed : Stack overflow.` What causes it, and what fixes it?

## Conventional answer

A stack overflow message with a 15-frame stack points at stack depth or at the code coverage collector. Removing coverage or forcing garbage collection should clear it. Source: the crash message itself, and the existing `LinqToMocksExpressionShouldBeValidAnalyzer` recursion entry in `.agents/skills/moq-analyzers-debugging-playbook/SKILL.md:139`, which is a real stack overflow with a different cause.

## First-principles position

The message names the symptom, not the cause. The 15 frames ended at `Thread.StartInternal`, so the host died while it started a thread, not while it recursed. On Windows, Roslyn Workspaces keeps document text in 8 MB memory mapped blocks (`TemporaryStorageService`). Those blocks count against the system commit limit and not against any process's private bytes. The parallel test framework started every test case in a class at once, so hundreds of workspaces held blocks together. When commit ran out, thread creation failed and the host reported a stack overflow.

## Evidence

- The Windows job crashed in 5 of 11 runs after #1484 (measured 2026-10-26).
- In a crashing run, commit owned by no process reached 32.7 GB. Free commit fell to 0 MB.
- Test host handles rose by about 1,100 while commit rose by about 8.5 GB. That is about 7.7 MB per handle.
- The crash reproduced with the coverage collector removed. Coverage is ruled out.
- One forced GC schedule was measured: a GC every 64 tests. It peaked at 13.1 GB, against 32.7 GB in the crashing run without it. The record does not show whether that run crashed or how close it came to the commit limit. Other schedules were not measured, so this does not show that GC cannot fix the crash.
- Limiting verifier runs in flight to 8 kept free commit above 10.1 GB in two Windows CI runs.
- The pre-#1484 code (4d31801) also fell to 369 MB of free commit without crashing. The Roslyn 4.8 to 5.0 upgrade pushed an existing risk past the limit.

## Decision

Keep `WindowsWorkspaceConcurrencyGate` (`tests/Moq.Analyzers.Test/Helpers/WindowsWorkspaceConcurrencyGate.cs`) at 8 runs in flight. Do not remove the coverage collector to fix this crash. Forced GC was tried on one schedule only (see Evidence), so treat it as untested beyond that schedule, not as ruled out. When a Windows test host reports `Stack overflow` with a stack that ends in thread start, sample free commit first. Raise the gate only with a new commit sample that shows headroom.

The gate and its cause are also noted at `.agents/skills/moq-analyzers-config-and-flags/SKILL.md:247` (checked at 72fd229c, 2026-11-25). No contradicted artifact remains, so no issue was filed.
