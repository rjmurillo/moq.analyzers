---
name: moq-api-reference
description: "Provides the verified Moq API surface map that moq.analyzers detection logic must match — load it when writing or reviewing anything that touches MoqKnownSymbols, `src/Common/ISymbolExtensions.Moq.cs`, or a rule that inspects Setup/Returns/Throws/Raises/Verify/Protected/It call chains. Triggers: \"what does Setup() return\", \"which interface declares Raises\", \"is this overload in Moq 4.8.2\", \"why does this KnownSymbols property resolve null\" (phantom symbols), \"T1..T16 in Returns\", \"It vs ItExpr\", \"ReturnsAsync not matching\" (GeneratedReturnsExtensions), \"Raise with EventArgs.Empty\", Net80WithOldMoq/Net80WithNewMoq test rows, or verifying any Moq API claim with dotnet-inspect. Do NOT load for general \"how do I use Moq in my tests\" questions (this is for analyzer authors, not Moq users), Roslyn API semantics (roslyn-analyzer-reference), rule-authoring steps (moq-analyzers-rule-lifecycle), or build/test mechanics (moq-analyzers-build-and-env)."
---

# Moq API reference for analyzer authors

This repo's analyzers make **symbol-based** decisions about user code that calls
Moq (ADR-001: detection compares Roslyn symbols against types/members resolved
from the referenced Moq assembly, never string names). That only works if the
symbol catalog matches what actually exists in Moq. Every table below was
verified by reflection against the two Moq versions the test suite pins
(4.8.2 and 4.18.4) on 2026-07-02. When in doubt, re-verify with the commands in
"How to verify any Moq API claim" — a plausible-but-wrong Moq fact here becomes
a false positive or a silently-dead code path in a shipped analyzer.

Jargon used throughout:

- **MoqKnownSymbols** (`src/Common/WellKnown/MoqKnownSymbols.cs`): the per-compilation
  catalog of Moq type and method symbols. Analyzers resolve Moq API through it, never
  via `Compilation.GetTypeByMetadataName` (banned in `BannedSymbols.txt`).
- **Metadata name**: the CLR name Roslyn uses for lookup, with arity backticks —
  ``Moq.Mock`1`` is `Mock<T>`, ``Moq.Language.IReturns`2`` is `IReturns<TMock, TResult>`.
- **Phantom symbol**: a MoqKnownSymbols entry whose metadata name does not exist in
  either supported Moq version, so it resolves `null`/empty forever (see the doctrine
  section — several are live in the catalog today).
- **FP / FN**: false positive (diagnostic on correct code) / false negative (missed
  diagnostic on wrong code).

## The two Moq versions and how tests pin them (2026-07-02)

`tests/Moq.Analyzers.Test/Helpers/ReferenceAssemblyCatalog.cs` defines four
reference-assembly groups; `TestDataExtensions` fans each data row across them:

| Catalog key | Contents | Why this version |
|---|---|---|
| `Net80` | .NET 8, **no Moq** | Analyzers must early-exit via `IsMockReferenced()` (`src/Common/WellKnown/MoqKnownSymbolExtensions.cs`) |
| `Net80WithOldMoq` | Moq **4.8.2** | Early popular version; deliberately older than 4.13.1, which changed `.As<T>()` internals (comment in the catalog file) |
| `Net80WithNewMoq` | Moq **4.18.4** | Most-downloaded version; ≥ 4.12.0 required for `Mock.Of<T>(MockBehavior)` (comment in the catalog file) |
| `Net80WithNewMoqAndLogging` | 4.18.4 + Microsoft.Extensions.Logging.Abstractions 8.0.0 | ILogger rules (Moq1004) |

`WithMoqReferenceAssemblyGroups()` = both Moq versions; `WithOldMoqReferenceAssemblyGroups()` /
`WithNewMoqReferenceAssemblyGroups()` = one. **Any detection change must state which
versions it applies to and be tested on both unless the API is 4.18.4-only** (see the
version diff table below for what is 4.18.4-only).

## Fluent chain map: what every call returns

Verified by reflection on both 4.8.2 and 4.18.4 (identical unless marked). All
interfaces live in `Moq.Language` or `Moq.Language.Flow` and carry
`[EditorBrowsable(EditorBrowsableState.Never)]` (verified) — IDE autocomplete and
docs hide them; do not trust memory, verify.

| User writes | Binds to (declaring type) | Static return type |
|---|---|---|
| `mock.Setup(x => x.Void())` | `Mock<T>.Setup(Expression<Action<T>>)` — **non-generic method** | `Moq.Language.Flow.ISetup<T>` (``ISetup`1``) |
| `mock.Setup(x => x.Get())` | `Mock<T>.Setup<TResult>(Expression<Func<T,TResult>>)` — **generic method** | `Moq.Language.Flow.ISetup<T,TResult>` (``ISetup`2``) |
| `mock.SetupGet(x => x.Prop)` | `Mock<T>.SetupGet<TProperty>` | `Moq.Language.Flow.ISetupGetter<T,TProperty>` (``ISetupGetter`2``) |
| `mock.SetupSet(x => x.Prop = v)` | 2 overloads | ``ISetupSetter`2`` (generic) or ``ISetup`1`` (non-generic) |
| `mock.SetupSequence(...)` | 2 overloads | ``ISetupSequentialResult`1`` / `ISetupSequentialAction` |
| `mock.SetupAdd/SetupRemove(...)` | **4.18.4 only** | ``ISetup`1`` |
| `mock.When(() => cond)` | `Mock<T>.When(Func<bool>)` | ``ISetupConditionResult`1`` |
| `.Returns(...)` / `.ReturnsAsync(...)` | ``IReturns`2`` / extension classes (below) | `Moq.Language.Flow.IReturnsResult<TMock>` (``IReturnsResult`1``) |
| `.Throws(...)` | `Moq.Language.IThrows` | `Moq.Language.Flow.IThrowsResult` |
| `.Raises(...)` | `Moq.Language.IRaise<T>` (``IRaise`1``) — the ONLY interface that declares it | `IVerifies` |
| `mock.Raise(...)` | `Mock<T>.Raise` — 2 overloads, both `void` | `void` |

Interface inheritance facts that gate what is chainable (verified both versions):

- ``ISetup`2`` (value-returning setup) implements ``IReturns`2``, ``IReturnsThrows`2``,
  ``ICallback`2``, `IThrows`, `IVerifies`.
- ``ISetup`1`` (void setup) implements `ICallback`, `ICallbackResult`, `IThrows`,
  `IThrowsResult`, `IOccurrence`, `IVerifies`, ``IRaise`1`` (4.18.4 adds `ICallBase`,
  `ICallBaseResult`). **No `IReturns*`** — void setups cannot chain `.Returns`.
- ``IReturnsResult`1`` implements `ICallback`, `IOccurrence`, ``IRaise`1``, `IVerifies` —
  it does **NOT** implement ``IReturns`2``. Consequence: `.Returns(x).Returns(y)` does
  not compile, and an analyzer walking a chain must not assume `Returns` is reachable
  after a `Returns`. `.Returns(x).Raises(...)` IS reachable (via ``IRaise`1``).
- `IThrowsResult` implements only `IOccurrence`, `IVerifies` — nothing chains
  `.Returns` after `.Throws` on a void setup.

### The #770 lesson: register the interface that DECLARES the member

`MoqKnownSymbols.CreateLazyMethods(type, name)` calls `type.GetMembers(name)`,
which returns **declared members only — it does not walk base interfaces**
(same as reflection `DeclaredOnly`; Roslyn `GetMembers` behaves identically for
interfaces). Verified 2026-07-02: `Raises` is declared on exactly one public
interface, ``IRaise`1`` (19 overloads, both versions), and on two internal 4.18.4-only
classes (``VoidSetupPhrase`1``, ``NonVoidSetupPhrase`2``, 19 each). It is declared on
NONE of ``ISetup`1``, ``ISetup`2``, ``ISetupGetter`2``, ``ISetupSetter`2``, `ICallback`,
``ICallback`2``, ``IReturnsResult`1`` — every `*Raises` registration against those
resolves **empty** even with Moq referenced.

History (git 35d363d, PR #770, closing the failed attempt documented in 5172cf3/#768):
removing the string-name fallback from `IsMoqRaisesMethod` broke 20 tests because
the symbol catalog was missing ``IRaise`1`` — the interface user `.Raises(...)` calls
actually bind to. The fix was registering ``IRaise`1``, not more endpoint interfaces.

Rules of thumb when registering a method group:

1. Find where the member is **declared** (not where it is reachable from) —
   `dotnet-inspect` or the reflection probe below.
2. Registration matching is `IsInstanceOf` (`src/Common/ISymbolExtensions.cs:34`):
   compares `OriginalDefinition`, unwraps `ReducedFrom` for extension methods, and
   `ConstructedFrom` for generic types. A call site binding `IRaise<IFoo>.Raises`
   matches a registration of ``IRaise`1.Raises`` — you do NOT need per-instantiation
   entries; you DO need the declaring interface.
3. Add a resolves-non-null/non-empty test with Moq referenced (see phantom doctrine).

## The Returns surface and the T1..T16 trap

`Moq.Language.IReturns<TMock, TResult>` (``IReturns`2``) declares `Returns` + `CallBase`.
Verified overload sets:

| Overload | 4.8.2 | 4.18.4 |
|---|---|---|
| `Returns(TResult value)` | yes | yes |
| `Returns(InvocationFunc valueFunction)` | **no** | yes — this is the 19→20 delta |
| `Returns(Delegate valueFunction)` | yes | yes |
| `Returns(Func<TResult>)` | yes | yes |
| `Returns<T1..T16>(Func<T1,...,T16,TResult>)` — 16 overloads | yes | yes |
| **Total** | **19** | **20** |

**TRAP:** in `Returns<T1,...,Tn>(Func<T1,...,Tn,TResult>)`, the generic parameters
`T1..T16` are the **mocked method's ARGUMENT types**; the return type is always the
setup's `TResult` (fixed by the ``IReturns`2`` instantiation). Code that reads
`method.TypeArguments.Last()` expecting the return type is plausible-but-wrong.
Related syntax note (authoring guidance): an explicit `.Returns<int>(...)` makes
`memberAccess.Name` a `GenericNameSyntax`, but most calls infer `T1..Tn`; and
target-typing converts the lambda, so to know what a `Returns` lambda produces,
analyze the **lambda body** (this is what `ReturnsDelegateShouldReturnTaskAnalyzer`
does), not the declared delegate type.

## ReturnsAsync lives in TWO static classes (root cause of #1243)

Verified on both 4.8.2 and 4.18.4:

| Class (metadata name) | `ReturnsAsync` overloads | Tracked in MoqKnownSymbols? |
|---|---|---|
| `Moq.ReturnsExtensions` | 10 (value/exception-based; also hosts `ThrowsAsync`) | yes — `ReturnsExtensionsReturnsAsync` / `...ThrowsAsync` |
| `Moq.GeneratedReturnsExtensions` | 30 (delegate-based: `ReturnsAsync(this IReturns<TMock,Task<TResult>>, Func<...>)` for arities up to 16) | **no** (as of 2026-07-02) |

Consequence: `setup.ReturnsAsync((MyValue v) => v)` resolves to a
`GeneratedReturnsExtensions` method that `IsInstanceOf(knownSymbols.ReturnsExtensionsReturnsAsync)`
cannot match. Today only the unconditional string-name fallback in
`MethodSetupShouldSpecifyReturnValueAnalyzer` (Moq1203) keeps that case green —
which is itself an ADR-001 violation producing FNs. Open issue **#1243** (sibling
of #1248) contains the full fix plan: register `GeneratedReturnsExtensions`, widen
`IsMoqReturnsAsyncMethod` (`src/Common/ISymbolExtensions.Moq.cs`), gate the name
fallback on `Symbol is null && CandidateSymbols.IsEmpty`. If you touch anything
ReturnsAsync-related, read #1243 first. (Per #1243's own verification,
`GeneratedReturnsExtensions` has no `ThrowsAsync` members in either version.)

## Raise / Raises legal call shapes (the #1248 FP context)

Verified on both 4.8.2 and 4.18.4 — the surface is identical:

- `Mock<T>.Raise` has exactly 2 overloads: `Raise(Action<T>, EventArgs)` and
  `Raise(Action<T>, params object[])`.
- ``IRaise`1.Raises`` has 19 overloads: `(Action<T>, EventArgs)`,
  `(Action<T>, Func<EventArgs>)`, `(Action<T>, params object[])`, and
  `(Action<T>, Func<T1,...,Tn,EventArgs>)` for n = 1..16.
  `RaisesAsync` has **0 overloads on ``IRaise`1``** in both supported versions.

Semantics: the `EventArgs` overload invokes handlers as `handler(mock.Object, args)`
— **Moq supplies the sender**. So for any "EventHandler-shaped" delegate
(`Invoke(object sender, TArgs e)` with `TArgs : EventArgs` — non-generic
`System.EventHandler`, `EventHandler<TArgs>` with EventArgs-derived `TArgs`, or a
custom delegate of that shape), BOTH of these are legal in BOTH versions:

```csharp
mock.Raise(m => m.Closed += null, EventArgs.Empty);              // 1-arg: Moq supplies sender
mock.Raise(m => m.Closed += null, this, EventArgs.Empty);        // 2-arg: params object[] form
```

Known live defect (audit A-3, issue **#1248**, open 2026-07-02): the shared helper
`src/Common/EventSyntaxExtensions.cs` special-cases only `EventHandler<T>`, so the
canonical 1-arg call on a **non-generic** `EventHandler` event is flagged
Moq1202/Moq1204 ("too few arguments"), and unresolvable delegate types are
conflated with zero-parameter delegates. Do not "fix" a test by asserting a
diagnostic on the canonical pattern above — that codifies the FP. #1248 has the
implementation plan (EventHandler-shaped detection with a `senderCanBeOmitted` flag).

## It vs ItExpr (and Moq1600)

Two matcher classes with deliberately different mechanics — verified members,
identical across 4.8.2/4.18.4 except the nested types noted:

| | `Moq.It` | `Moq.Protected.ItExpr` |
|---|---|---|
| Returns | `TValue` (evaluates to `default(T)` at runtime; recognized inside expression trees) | `System.Linq.Expressions.Expression` (a matcher expression object) |
| Members | 7 method names: `IsAny`, `IsNotNull`, `Is`, `IsInRange`, `IsIn`, `IsNotIn`, `IsRegex` | 5 method names / 6 overloads: `IsNull`, `IsAny`, `Is`, `IsInRange`, `IsRegex`(×2) |
| Asymmetry | has `IsNotNull`, **no `IsNull`** | has `IsNull`, **no `IsNotNull`** |
| Nested types | `Ref<TValue>` (both versions); `IsAnyType`, `IsSubtype<T>`, `IsValueType` (4.18.4 only) | `Ref<TValue>` |

Why it matters (**Moq1600**, `ProtectedSetupShouldUseItExprAnalyzer`,
`docs/rules/Moq1600.md`): the string-based protected API
(`mock.Protected().Setup<bool>("Foo", …)`) takes `object[]` arguments evaluated
eagerly. An `It.IsAny<string>()` there compiles but evaluates to `default(string)`
and is treated as a literal argument, not a matcher — the setup silently fails to
match at runtime. `ItExpr` matchers must be used in string-based protected
setups/verifies. The lambda-based route `mock.Protected().As<TInterface>().Setup(m => …)`
(via ``IProtectedAsMock`2``, present in both versions) correctly uses regular `It`
and must NOT be flagged.

**`It.Ref<T>.IsAny` containing-type trap:** it is a public static **field** on the
nested class ``Moq.It+Ref`1``. Its `ContainingType` is `It.Ref<T>` — NOT `Moq.It` —
so a naive `SymbolEqualityComparer.Equals(field.ContainingType, knownSymbols.It)`
misses it, and it is a field reference, not an invocation. The correct handling is
the worked example at `src/Analyzers/ProtectedSetupShouldUseItExprAnalyzer.cs:222-240`
(`IsContainedInMoqIt`): match the containing type OR its `ContainingType` against
`Moq.It`. Test rows in #1270 use `ref It.Ref<string>.IsAny` extensively — keep
that pattern for ref/out/in setups in fixtures.

## Version diff: Moq 4.8.2 vs 4.18.4 (verified 2026-07-02)

| Surface | 4.8.2 | 4.18.4 |
|---|---|---|
| ``IReturns`2.Returns`` overloads | 19 | 20 (adds `Returns(InvocationFunc)`) |
| `IThrows.Throws` overloads | 2 | 20 (delegate-based family added) |
| ``IProtectedMock`1`` declared methods | 14 | 25 (same 8 names: As, Setup, SetupGet, SetupSet, SetupSequence, Verify, VerifyGet, VerifySet — more overloads, incl. `exactParameterMatch` forms) |
| `Mock<T>.SetupAdd` / `SetupRemove` | **absent** | present (→ ``ISetup`1``) |
| `Mock.Of` overloads | 2: `Of<T>()`, `Of<T>(Expression)` | 4: adds `Of<T>(MockBehavior)`, `Of<T>(Expression, MockBehavior)` (added in Moq 4.12.0 per catalog comment) |
| `It` nested types | `Ref<TValue>` only | + `IsAnyType`, `IsSubtype<T>`, `IsValueType` |
| ``VoidSetupPhrase`1`` / ``NonVoidSetupPhrase`2`` (internal) | absent | present |
| `ReturnsExtensions.ReturnsAsync` / `GeneratedReturnsExtensions.ReturnsAsync` | 10 / 30 | 10 / 30 (identical) |
| `Mock<T>.Raise`, ``IRaise`1.Raises`` shapes | identical | identical |
| `ItExpr` members | identical | identical |

Implications for tests: rows exercising SetupAdd/SetupRemove, `Mock.Of(MockBehavior)`,
`It.IsAnyType`, or the delegate-based `Throws` family must use
`WithNewMoqReferenceAssemblyGroups()`; everything else should run on both groups.

## PHANTOM SYMBOLS doctrine

`MoqKnownSymbols` contains entries whose metadata names do not exist in EITHER
supported Moq version. They resolve `null` (types) or empty (method groups) with
Moq fully referenced. Verified by reflection 2026-07-02:

| MoqKnownSymbols property | Registered metadata name | Reality in Moq 4.8.2/4.18.4 |
|---|---|---|
| `IReturns` | `Moq.Language.IReturns` | does not exist (only ``IReturns`2``) |
| `IReturns1` | ``Moq.Language.IReturns`1`` | does not exist |
| `ICallback1` | ``Moq.Language.ICallback`1`` | does not exist (only `ICallback`, ``ICallback`2``) |
| `ISetupGetter` | ``Moq.Language.ISetupGetter`2`` | wrong namespace — real type is ``Moq.Language.Flow.ISetupGetter`2`` |
| `ISetupSetter` | ``Moq.Language.ISetupSetter`2`` | wrong namespace — real type is ``Moq.Language.Flow.ISetupSetter`2`` |
| `ISetupPhrase1` | ``Moq.Language.Flow.ISetupPhrase`1`` | does not exist (real internal classes: `SetupPhrase`, ``VoidSetupPhrase`1``, ``NonVoidSetupPhrase`2``) |
| `ISetupGetter1` / `ISetupSetter1` | ``Moq.Language.Flow.ISetupGetter`1`` / ``...ISetupSetter`1`` | wrong arity — real interfaces have arity 2 |
| `IRaiseable` / `IRaiseableAsync` | `Moq.Language.IRaiseable(Async)` | absent from both supported versions; the test comment says they were added after 4.18.4 (UNVERIFIED against newer Moq — no newer package available to check) |
| `IRaise1RaisesAsync` (method group) | `RaisesAsync` on ``IRaise`1`` | 0 overloads in both versions — always empty |

Additionally, every `*Raises` method-group registration EXCEPT `IRaise1Raises`,
`VoidSetupPhrase1Raises`, and `NonVoidSetupPhrase2Raises` resolves empty because
`Raises` is not *declared* on those types (see the #770 lesson). The net effect:
`IsMoqRaisesMethod` (`src/Common/ISymbolExtensions.Moq.cs:176`) checks 18 symbol
groups across its five private helpers, of which only 3 can match: `IRaise1Raises`
(both versions) and the two concrete phrase classes (4.18.4). The dead checks are harmless (empty arrays match
nothing) but they are landmines for reasoning: "the analyzer checks
`ISetupGetterRaises`" is technically true and behaviorally false.

What the test suite pins today (2026-07-02): `MoqKnownSymbolsTests.RaisesAndEvents.cs`
explicitly asserts `IRaiseable` is null and `IRaiseableRaises` is empty **with Moq
4.18.4 referenced**. The other phantoms are only pinned in the no-Moq configuration
(trivially null) — the with-Moq nullness is verified by this skill's reflection
probe, not by a test.

**Doctrine — non-negotiable when you register a new symbol:**

1. Verify the exact metadata name (namespace + arity) with `dotnet-inspect` or the
   reflection probe against BOTH package versions. Never transcribe from memory or
   from Moq's docs site.
2. Add BOTH tests to `tests/Moq.Analyzers.Test/Common/MoqKnownSymbolsTests.*.cs`:
   without-Moq → null/empty, AND **with-Moq → NotNull/NonEmpty** (pattern:
   `IReturns2_WithMoqReference_ReturnsNamedTypeSymbol` in
   `MoqKnownSymbolsTests.ReturnsAndThrows.cs:100`). The with-Moq test is the one
   that catches phantoms. If a symbol is deliberately forward-looking (absent from
   4.18.4), pin the null like `IRaiseable_WithMoqReference_ReturnsNullForMoq4` does,
   with a comment saying which Moq version introduces it.
3. Do not delete existing phantom entries as a drive-by: some are deliberate
   forward-compat (`IRaiseable`), some back real (if currently unreachable) defense
   layers. Removing or re-pointing them is a detection-behavior change — treat it
   per moq-analyzers-change-control, with symbol-coverage proof (the 5172cf3 → 35d363d
   history is the cautionary tale).

## Other detection-relevant Moq facts

- **`IsMoqSetupMethod` requires `IsGenericMethod`** (`src/Common/ISymbolExtensions.Moq.cs:42`):
  `Mock<T>.Setup(Expression<Action<T>>)` — every void-member setup — is non-generic,
  so it is never analyzed today. Known FN, pinned as commented rows in issue #1270.
  The same `IsGenericMethod` gate exists on `IsMoqSetupSequenceMethod` (line 62).
- **`MockBehavior`** is an enum with fields `Strict`, `Loose`, `Default` (`Default`
  aliases `Loose`); MoqKnownSymbols resolves the fields via `CreateLazySingleField`.
  Compare argument constants by **value** (`Equals` on `ConstantValue`), not `==`
  on boxed objects (audit A-10).
- **`MockRepository`** derives from obsolete `MockFactory`; `Create`/`Verify` are
  registered with `CreateLazyInheritedMethods`, which DOES walk base types — the
  one place the declared-members-only caveat is already handled.
- **`Times`** is a struct; `AtLeastOnce`/`Never`/`Once`/`Exactly` are methods, and
  `Verify` also has `Func<Times>` overloads (method-group form `Times.Once` without
  parens) in both versions — a known Moq1420 coverage gap (#1270).
- **User subclasses of `Mock<T>`** (`class MyMock : Mock<IFoo>`) are not detected by
  mock-creation rules (exact `ConstructedFrom` comparison) — possibly intentional
  scope; do not "fix" casually.
- **DoppelgangerTestHelper**: user-defined look-alike `Mock<T>`/`MockBehavior` types
  in a different assembly must NOT trigger any rule — every new detection needs a
  doppelganger test.

## How to verify any Moq API claim (do this before writing detection code)

### Option A: dotnet-inspect (network required once per package; v0.16.0 verified 2026-07-02)

```bash
dotnet tool install -g dotnet-inspect          # once; ensure ~/.dotnet/tools is on PATH
```

Syntax notes that cost real time (all verified):

- Moq's fluent interfaces are hidden (`Moq.Language*`), so **`--all` is required**
  or you get "Type not found".
- Generic type names: either spacing works for `member` on dotnet-inspect 0.16.0
  (`"IReturns<TMock,TResult>"` and `"IReturns<TMock, TResult>"` both resolve,
  re-tested 2026-07-02); prefer the unspaced form for shell-quoting safety.
- Pin versions with `@`: `--package Moq@4.8.2`.

```bash
# List a version's public root types (Moq.Language.* will NOT appear here):
dotnet-inspect type Moq@4.18.4

# Overloads of Returns on the fluent interface, per version:
dotnet-inspect member "IReturns<TMock,TResult>" --package Moq@4.18.4 --all -m Returns
dotnet-inspect member "IReturns<TMock,TResult>" --package Moq@4.8.2  --all -m Returns

# ItExpr surface (public, no --all needed):
dotnet-inspect member ItExpr --package Moq@4.8.2

# Full API diff between the two supported versions:
dotnet-inspect diff Moq@4.8.2..4.18.4
```

Caveat: `dotnet-inspect type` counts only public types and `diff` reports the
public surface — internal types (e.g. ``VoidSetupPhrase`1``) and hidden-namespace
declarations need Option B.

### Option B: reflection probe against the cached packages (offline, authoritative)

The Roslyn testing SDK installs the pinned reference packages under the temp
directory; in this environment (verified 2026-07-02) the DLLs are at:

```text
/tmp/test-packages/moq/4.8.2/lib/netstandard1.3/Moq.dll
/tmp/test-packages/moq/4.18.4/lib/net6.0/Moq.dll
```

(after at least one test run; normally-restored copies also land under
`dotnet nuget locals global-packages --list`). Probe with a throwaway console app
using `System.Reflection.MetadataLoadContext` (add the NuGet package of the same
name); load framework DLLs from `RuntimeEnvironment.GetRuntimeDirectory()` plus the
Moq lib folder into a `PathAssemblyResolver` (dedupe by simple name or the context
throws "already loaded"). Then:

```csharp
var t = asm.GetType("Moq.Language.IReturns`2");           // null => phantom name
int n = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
         .Count(m => m.Name == "Returns");                 // DeclaredOnly mirrors Roslyn GetMembers
```

`DeclaredOnly` is the load-bearing flag: it answers the same question Roslyn's
`GetMembers` answers for `MoqKnownSymbols` registrations (declared, not inherited).
Build this probe in a temporary directory outside the repo (e.g.
`cd "$(mktemp -d)"`), never inside the repo working tree — a stray `.csproj`
breaks the solution build and hooks.

### Option C: pin it in the test suite

For anything an analyzer depends on, the durable form is a `MoqKnownSymbolsTests`
fact (with-Moq NotNull/NonEmpty) — reflection and dotnet-inspect verify today;
the test keeps it verified.

## When NOT to use this skill

- General Moq usage ("how do I mock X in my test project") — this skill documents
  Moq as a **detection target** for analyzer authors, not as a testing tool.
- Roslyn API semantics (`GetSymbolInfo`, operations vs syntax, spans) →
  **roslyn-analyzer-reference**.
- End-to-end steps to add/modify a rule (files, AnalyzerReleases, docs, benchmarks) →
  **moq-analyzers-rule-lifecycle**.
- Build/test/format commands, SDK setup, package cache issues →
  **moq-analyzers-build-and-env**.
- Why a specific test/CI check fails → **moq-analyzers-debugging-playbook**.
- Design invariants, ADRs, banned APIs → **moq-analyzers-architecture-contract**.
- PR gates, release promotion, who may change what → **moq-analyzers-change-control**.
- Settled detection-strategy history (string→symbol war, Moq1203 saga) →
  **moq-analyzers-failure-archaeology**.
- Burning down the FP backlog (#1241–#1278 campaign) →
  **moq-analyzers-fp-convergence-campaign**.

## Provenance and maintenance

Re-verification one-liners for everything volatile in this skill:

- Moq versions pinned by tests: `grep -n "PackageIdentity(\"Moq\"" tests/Moq.Analyzers.Test/Helpers/ReferenceAssemblyCatalog.cs`
- Returns overload counts (19 vs 20): `dotnet-inspect member "IReturns<TMock,TResult>" --package Moq@4.8.2 --all -m Returns | grep -c 'Returns('` (and `@4.18.4`)
- Throws overload counts (2 vs 20): `dotnet-inspect member IThrows --package Moq@4.8.2 --all -m Throws` (and `@4.18.4`)
- GeneratedReturnsExtensions still untracked: `grep -c GeneratedReturnsExtensions src/Common/WellKnown/MoqKnownSymbols.cs` (0 as of 2026-07-02; issue #1243 open — becomes stale when #1243 lands)
- Phantom entries still present: ``grep -n 'Moq.Language.IReturns"\|IReturns`1\|ICallback`1\|Moq.Language.ISetupGetter`2\|ISetupPhrase`1' src/Common/WellKnown/MoqKnownSymbols.cs``
- Raises declared only on ``IRaise`1``: rerun the Option B probe with `DeclaredOnly` over ``Moq.Language.Flow.ISetup`1`` etc.
- EventHandler Raise FP still live: check issue #1248 state (`gh issue view 1248 -R rjmurillo/moq.analyzers`); if closed, the Moq1202/Moq1204 FP paragraphs need updating
- Void-Setup FN still live: `grep -n "IsGenericMethod" src/Common/ISymbolExtensions.Moq.cs` and issue #1270 state
- IsMoqRaisesMethod symbol-group list: `grep -n "IsInstanceOf(knownSymbols" src/Common/ISymbolExtensions.Moq.cs`
- With-Moq phantom pins in tests: `grep -rn "WithMoqReference_ReturnsNull" tests/Moq.Analyzers.Test/Common/`
- dotnet-inspect version/flags: `dotnet-inspect --version` (syntax notes verified on 0.16.0)
- Test package cache location: `ls /tmp/test-packages/moq/` after a test run (environment-specific; falls back to `dotnet nuget locals global-packages --list`)

- Frontmatter stays parser-safe: `python3 -c "import yaml; print(len(yaml.safe_load(open('.github/skills/moq-api-reference/SKILL.md').read().split('---')[1])['description']))"` — expect the full description length, not an error or a truncated count

Last verified: 2026-07-02 against commit 05135b2.
