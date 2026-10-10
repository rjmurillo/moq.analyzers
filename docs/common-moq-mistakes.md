# Common Moq mistakes and the Moq.Analyzers rules that catch them

This page lists common mistakes developers make with [Moq](https://github.com/devlooped/moq). Each mistake maps
to a [Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers) rule that reports it at compile time, before the test
runs. Make sure you use a
[supported version of the .NET SDK](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). Then install
the analyzer in your test project:

```shell
dotnet add package Moq.Analyzers
```

Moq.Analyzers reports the rules below. It does not catch every Moq mistake, and it does not replace your tests.

## Contents

- [All rules at a glance](#all-rules-at-a-glance)
- [Mocking a sealed class](#mocking-a-sealed-class)
- [Setting up or verifying a non-virtual member](#setting-up-or-verifying-a-non-virtual-member)
- [Async setups done wrong](#async-setups-done-wrong)
- [Callback parameters that do not match](#callback-parameters-that-do-not-match)
- [Constructor argument mistakes](#constructor-argument-mistakes)
- [Mocking internal types without InternalsVisibleTo](#mocking-internal-types-without-internalsvisibleto)
- [Mocking ILogger](#mocking-ilogger)
- [Property and method setup confusion](#property-and-method-setup-confusion)
- [Missing return values](#missing-return-values)
- [Event mistakes](#event-mistakes)
- [Mock.As, Mock.Get, and LINQ to Mocks misuse](#mockas-mockget-and-linq-to-mocks-misuse)
- [Relying on default Loose behavior](#relying-on-default-loose-behavior)
- [Verification hygiene](#verification-hygiene)
- [Protected setups without ItExpr](#protected-setups-without-itexpr)
- [Install and configure](#install-and-configure)
- [FAQ](#faq)

## All rules at a glance

| Mistake | Rule ID | Title | Category | Default severity | Code fix |
| ------- | ------- | ----- | -------- | ---------------- | -------- |
| You mock a sealed class | [Moq1000](rules/Moq1000.md) | Sealed classes cannot be mocked | Usage | Warning | No |
| You pass constructor arguments to an interface mock | [Moq1001](rules/Moq1001.md) | Mocked interfaces cannot have constructor parameters | Usage | Warning | No |
| You pass constructor arguments that match no constructor | [Moq1002](rules/Moq1002.md) | Parameters provided into mock do not match any existing constructors | Usage | Warning | No |
| You mock an internal type without `InternalsVisibleTo` | [Moq1003](rules/Moq1003.md) | Internal type requires InternalsVisibleTo for DynamicProxy | Usage | Warning | No |
| You mock `ILogger` or `ILogger<T>` | [Moq1004](rules/Moq1004.md) | ILogger should not be mocked | Usage | Warning | No |
| Your `Callback` parameters differ from the method you set up | [Moq1100](rules/Moq1100.md) | Callback signature must match the signature of the mocked method | Correctness | Warning | Yes |
| You use `SetupGet`, `SetupSet`, or `SetupProperty` on a method | [Moq1101](rules/Moq1101.md) | SetupGet/SetupSet/SetupProperty should be used for properties, not for methods | Correctness | Warning | No |
| You call `Setup` on a non-virtual member | [Moq1200](rules/Moq1200.md) | Setup should be used only for overridable members | Correctness | Error | No |
| You put `.Result` inside a `Setup` expression (Moq older than 4.16.0) | [Moq1201](rules/Moq1201.md) | Setup of async methods should use `.ReturnsAsync` instance instead of `.Result` | Correctness | Error | No |
| You pass the wrong arguments to `Mock.Raise` | [Moq1202](rules/Moq1202.md) | Raise event arguments should match the event delegate signature | Correctness | Warning | No |
| You set up a method and never say what it returns | [Moq1203](rules/Moq1203.md) | Method setup should specify a return value | Correctness | Warning | No |
| You pass the wrong arguments to `Raises` | [Moq1204](rules/Moq1204.md) | Raises event arguments should match event signature | Correctness | Warning | No |
| Your `SetupAdd` or `SetupRemove` handler type differs from the event type, which the compiler also rejects (CS0029) | [Moq1205](rules/Moq1205.md) | Event setup handler type should match event delegate type | Correctness | Warning | No |
| You pass an `async` lambda to `Returns` | [Moq1206](rules/Moq1206.md) | Async method setups should use ReturnsAsync instead of Returns with async lambda | Correctness | Warning | No |
| You call `SetupSequence` on a non-virtual member | [Moq1207](rules/Moq1207.md) | SetupSequence should be used only for overridable members | Correctness | Error | No |
| Your `Returns` delegate returns `int` for a `Task<int>` method | [Moq1208](rules/Moq1208.md) | Returns() delegate type mismatch on async method setup | Correctness | Warning | Yes |
| You call `Verify` on a non-virtual member | [Moq1210](rules/Moq1210.md) | Verify should be used only for overridable members | Correctness | Error | Yes |
| You pass a class to `Mock.As<T>()` | [Moq1300](rules/Moq1300.md) | `Mock.As()` should take interfaces only | Usage | Error | No |
| You pass a literal to `Mock.Get()` | [Moq1301](rules/Moq1301.md) | Mock.Get() should not take literals | Usage | Warning | No |
| You use a non-virtual member in `Mock.Of<T>()` | [Moq1302](rules/Moq1302.md) | LINQ to Mocks expression should be valid | Usage | Warning | No |
| You rely on the default `Loose` behavior | [Moq1400](rules/Moq1400.md) | Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior | Best Practice | Warning | Yes |
| You do not use `MockBehavior.Strict` | [Moq1410](rules/Moq1410.md) | Explicitly set the Strict mocking behavior | Best Practice | Info | Yes |
| You pass `Times.AtLeastOnce()` to `Verify` | [Moq1420](rules/Moq1420.md) | Redundant `Times.AtLeastOnce()` specification can be removed | Usage | Info | No |
| You create mocks from a `MockRepository` and never call `Verify()` | [Moq1500](rules/Moq1500.md) | MockRepository.Verify() should be called | Best Practice | Warning | No |
| You use `It` matchers in a string-based protected setup | [Moq1600](rules/Moq1600.md) | Protected setup should use `ItExpr` matchers | Usage | Warning | No |

The [rule index](rules/README.md) lists the same rules with their implementation files.

## Mocking a sealed class

Moq cannot mock a sealed class, and [Moq1000](rules/Moq1000.md) flags the attempt. Moq builds a mock by generating a
subclass. A sealed class cannot have one.

```csharp
public sealed class MyClass { }

var mock = new Mock<MyClass>(); // Moq1000: Sealed classes cannot be mocked
```

You have three fixes: introduce an interface and mock that, use the real class, or unseal the class.

```csharp
public class MyClass { }

var mock = new Mock<MyClass>();
```

Without the analyzer, Moq 4.18.4 throws `NotSupportedException` at run time with this message:

```text
Type to mock ({0}) must be an interface, a delegate, or a non-sealed, non-static class.
```

Moq replaces `{0}` with the type name. See [Moq1000](rules/Moq1000.md).

## Setting up or verifying a non-virtual member

Moq can only intercept `virtual`, `abstract`, and interface members. Three rules flag a non-virtual member at compile
time: [Moq1200](rules/Moq1200.md) for `Setup`, [Moq1207](rules/Moq1207.md) for `SetupSequence`, and
[Moq1210](rules/Moq1210.md) for `Verify`. All three default to Error.

```csharp
public class SampleClass
{
    public int Property { get; set; }
    public int Method() => 42;
}

var mock = new Mock<SampleClass>();

mock.Setup(x => x.Property);         // Moq1200: Setup should be used only for overridable members
mock.SetupSequence(x => x.Method()); // Moq1207: SetupSequence should be used only for overridable members
mock.Verify(x => x.Method());        // Moq1210: Verify should be used only for overridable members
```

Make the member `virtual`, or mock an interface instead of the class.

```csharp
public class SampleClass
{
    public virtual int Property { get; set; }
    public virtual int Method() => 42;
}

var mock = new Mock<SampleClass>();

mock.Setup(x => x.Property);
mock.SetupSequence(x => x.Method());
mock.Verify(x => x.Method());
```

Moq1210 has a code fix that adds `virtual`. The fix is not offered for static members, members of sealed classes,
struct members, or interface members.

### The runtime error this replaces

The exact text depends on your Moq version. Moq replaces each `{0}` with the expression or the member name.

Moq 4.2 through 4.10.1 (checked at 4.2.1510.2205, 4.5.30, 4.7.145, 4.9.0, and 4.10.1):

```text
Invalid setup on a non-virtual (overridable in VB) member: {0}
Invalid verify on a non-virtual (overridable in VB) member: {0}
```

Moq 4.11.0 through 4.20.72 (checked at 4.11.0, 4.18.4, and 4.20.72). Moq 4.18.4 throws it as
`NotSupportedException`:

```text
Unsupported expression: {0}
Non-overridable members (here: {0}) may not be used in setup / verification expressions.
```

### Sealed default interface members

An interface member is normally overridable. A `sealed` default interface member is not, and Moq cannot intercept it.
Moq1200, Moq1207, and Moq1210 report it.

```csharp
public interface IService
{
    sealed void Save() { }
}

new Mock<IService>().Setup(x => x.Save()); // Moq1200
```

## Async setups done wrong

Use `ReturnsAsync` to set up an async method. Three rules flag the common wrong patterns:
[Moq1201](rules/Moq1201.md), [Moq1206](rules/Moq1206.md), and [Moq1208](rules/Moq1208.md).

| Wrong pattern | Rule |
| ------------- | ---- |
| `.Result` inside the `Setup` expression | [Moq1201](rules/Moq1201.md) |
| `Returns` with an `async` lambda | [Moq1206](rules/Moq1206.md) |
| `Returns` with a delegate that returns `T` for a `Task<T>` or `ValueTask<T>` method | [Moq1208](rules/Moq1208.md) |

```csharp
public interface IService
{
    Task<int> GetValueAsync();
    Task<string> GetNameAsync();
}

var mock = new Mock<IService>();

mock.Setup(x => x.GetNameAsync().Result);                      // Moq1201 (Moq older than 4.16.0)
mock.Setup(x => x.GetNameAsync()).Returns(async () => "name"); // Moq1206
mock.Setup(x => x.GetValueAsync()).Returns(() => 42);          // Moq1208
```

Moq1201 reports only when the project references a Moq version older than 4.16.0. The analyzer skips the rule for
Moq 4.16.0 and later.

`ReturnsAsync` fixes all three.

```csharp
var mock = new Mock<IService>();

mock.Setup(x => x.GetNameAsync()).ReturnsAsync("name");
mock.Setup(x => x.GetValueAsync()).ReturnsAsync(42);
```

You can also keep `Returns` and wrap the value yourself: `Returns(() => Task.FromResult(42))`.

Moq1208 has a code fix that rewrites `Returns` to `ReturnsAsync`. The Moq1208 pattern compiles. Without the analyzer,
Moq 4.18.4 then throws `ArgumentException` at run time with this message:

```text
Invalid callback. Setup on method with return type '{0}' cannot invoke callback with return type '{1}'.
```

## Callback parameters that do not match

The parameters of `.Callback()` must match the method in `.Setup()`, and [Moq1100](rules/Moq1100.md) flags a mismatch.
A code fix corrects the callback signature for you.

```csharp
public interface IMyService
{
    int Do(int i, string s, DateTime dt);
}

var mock = new Mock<IMyService>();

mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback((string s1, int i1) => { }) // Moq1100: Callback signature must match the signature of the mocked method
    .Returns(0);
```

```csharp
var mock = new Mock<IMyService>();

mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback((int i, string s, DateTime dt) => { })
    .Returns(0);
```

Without the analyzer, Moq 4.18.4 throws `ArgumentException` at run time with this message:

```text
Invalid callback. Setup on method with parameters ({0}) cannot invoke callback with parameters ({1}).
```

A callback for a `ref` or `out` parameter needs a custom delegate type. See [Moq1100](rules/Moq1100.md) for those
patterns.

## Constructor argument mistakes

Arguments you pass to `new Mock<T>(...)` go to the constructor of `T`. Two rules check them:
[Moq1001](rules/Moq1001.md) flags any argument on an interface mock, and [Moq1002](rules/Moq1002.md) flags arguments
that match no constructor.

```csharp
public interface IMyService
{
    void Do(string s);
}

public class MyClass
{
    public MyClass(string s) { }
}

var mock1 = new Mock<IMyService>("123"); // Moq1001: Mocked interfaces cannot have constructor parameters
var mock2 = new Mock<MyClass>(3);        // Moq1002: Parameters provided into mock do not match any existing constructors
```

```csharp
var mock1 = new Mock<IMyService>();
var mock2 = new Mock<MyClass>("three");
```

Without the analyzer, Moq 4.18.4 reports these messages at run time:

```text
Constructor arguments cannot be passed for interface mocks.
A matching constructor for the given arguments was not found on the mocked type.
```

## Mocking internal types without InternalsVisibleTo

Moq needs access to an `internal` type to generate a proxy for it, and [Moq1003](rules/Moq1003.md) flags a mock of an
internal type that lacks that access. Castle DynamicProxy builds the proxy in an assembly named
`DynamicProxyGenAssembly2`.

```csharp
// Assembly without InternalsVisibleTo
internal class MyService { }

var mock = new Mock<MyService>(); // Moq1003: Internal type requires InternalsVisibleTo
```

Add the attribute to the assembly that contains the internal type.

```csharp
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]

internal class MyService { }

var mock = new Mock<MyService>();
```

You can also make the type `public`, or mock a public interface instead. See [Moq1003](rules/Moq1003.md).

## Mocking ILogger

Do not mock `ILogger` or `ILogger<T>`. [Moq1004](rules/Moq1004.md) flags these mocks because they are fragile and
you do not need them.

```csharp
using Microsoft.Extensions.Logging;

var mock = new Mock<ILogger<MyService>>();   // Moq1004
var logger = Mock.Of<ILogger>();             // Moq1004
```

Use `NullLogger` when the test ignores logging.

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

ILogger logger = NullLogger.Instance;
ILogger<MyService> typedLogger = NullLogger<MyService>.Instance;
```

Use `FakeLogger` from the `Microsoft.Extensions.Diagnostics.Testing` package when the test verifies log output.

```csharp
using Microsoft.Extensions.Logging.Testing;

var fakeLogger = new FakeLogger<MyService>();
var service = new MyService(fakeLogger);

service.Save();

Assert.Equal(LogLevel.Information, fakeLogger.LatestRecord.Level);
Assert.Equal("Saved", fakeLogger.LatestRecord.Message);
Assert.Equal(1, fakeLogger.Collector.Count);
```

This replaces `mock.Verify(x => x.Log(...))` on an `ILogger` mock. `LatestRecord` holds the last entry, and
`Collector.GetSnapshot()` returns every entry.

See [Moq1004](rules/Moq1004.md). Its
[Tests that verify logging](rules/Moq1004.md#tests-that-verify-logging-fakelogger) section replaces a `Verify` call step by step.

## Property and method setup confusion

`SetupGet`, `SetupSet`, and `SetupProperty` work on properties only. [Moq1101](rules/Moq1101.md) flags them when the
expression is a method call.

```csharp
public interface IMyInterface
{
    string Name { get; }
    string Method();
}

var mock = new Mock<IMyInterface>();

mock.SetupGet(x => x.Method()); // Moq1101: SetupGet/SetupSet/SetupProperty should be used for properties, not for methods
```

Use `Setup` for a method. Keep `SetupGet` for a property.

```csharp
var mock = new Mock<IMyInterface>();

mock.Setup(x => x.Method()).Returns("value");
mock.SetupGet(x => x.Name).Returns("name");
```

See [Moq1101](rules/Moq1101.md).

## Missing return values

A setup for a method that returns a value must say what it returns, and [Moq1203](rules/Moq1203.md) flags a setup that
does not. A `Callback` alone does not count.

```csharp
public interface IFoo
{
    int GetValue();
    Task<int> BarAsync();
}

var mock = new Mock<IFoo>();

mock.Setup(x => x.GetValue());                     // Moq1203: Method setup should specify a return value
mock.Setup(x => x.BarAsync());                     // Moq1203
mock.Setup(x => x.GetValue()).Callback(() => { }); // Moq1203
```

Finish the setup with `Returns`, `ReturnsAsync`, `Throws`, or `ThrowsAsync`.

```csharp
var mock = new Mock<IFoo>();

mock.Setup(x => x.GetValue()).Returns(42);
mock.Setup(x => x.BarAsync()).ReturnsAsync(1);
mock.Setup(x => x.GetValue()).Callback(() => { }).Returns(42);
```

The rule skips `void` methods and property setups. See [Moq1203](rules/Moq1203.md).

## Event mistakes

Event arguments must match the event delegate. [Moq1202](rules/Moq1202.md) checks `Mock.Raise` and
[Moq1204](rules/Moq1204.md) checks `Raises`. Both mismatches compile and then fail at run time.
[Moq1205](rules/Moq1205.md) covers the handler type in `SetupAdd` and `SetupRemove`, where the C# compiler already
rejects a mismatch.

```csharp
public interface INotifier
{
    void Submit();
    event Action<string> Completed;
}

var mock = new Mock<INotifier>();

mock.Raise(x => x.Completed += null, 42);                         // Moq1202: int passed, string expected
mock.Setup(x => x.Submit()).Raises(x => x.Completed += null, 42); // Moq1204: int passed, string expected
```

```csharp
var mock = new Mock<INotifier>();

mock.Raise(x => x.Completed += null, "done");
mock.Setup(x => x.Submit()).Raises(x => x.Completed += null, "done");
mock.SetupAdd(x => x.Completed += It.IsAny<Action<string>>());
```

A wrong handler type such as `It.IsAny<Action<int>>()` in that `SetupAdd` call does not compile. The C# compiler
reports CS0029 before Moq1205 can.

For an `EventHandler` event you can pass only the `EventArgs`. Moq supplies the sender. See
[Moq1202](rules/Moq1202.md), [Moq1204](rules/Moq1204.md), and [Moq1205](rules/Moq1205.md).

## Mock.As, Mock.Get, and LINQ to Mocks misuse

Three rules cover three helper APIs: [Moq1300](rules/Moq1300.md) for `Mock.As<T>()`, [Moq1301](rules/Moq1301.md) for
`Mock.Get()`, and [Moq1302](rules/Moq1302.md) for `Mock.Of<T>()`.

`As<T>()` adds an interface to a mock, so `T` must be an interface. Moq1300 defaults to Error.

```csharp
public interface ISampleInterface
{
    int Calculate(int a, int b);
}

public class SampleClass { }

var bad = new Mock<SampleClass>().As<SampleClass>();       // Moq1300: Mock.As() should take interfaces only
var good = new Mock<SampleClass>().As<ISampleInterface>();
```

Without the analyzer, Moq 4.18.4 throws `ArgumentException` with the message
`Can only add interfaces to the mock.`

`Mock.Get()` returns the mock behind a mocked object. A literal has no mock behind it.

```csharp
var bad = Mock.Get("literal string"); // Moq1301: Mock.Get() should not take literals

var mockObject = new Mock<IMyInterface>().Object;
var good = Mock.Get(mockObject);
```

A LINQ to Mocks expression can only use `virtual`, `abstract`, or interface members.

```csharp
public class ConcreteClass
{
    public string NonVirtualProperty { get; set; }
}

public interface IService
{
    string Name { get; }
}

var bad = Mock.Of<ConcreteClass>(c => c.NonVirtualProperty == "test"); // Moq1302
var good = Mock.Of<IService>(s => s.Name == "test");
```

## Relying on default Loose behavior

A mock uses `MockBehavior.Loose` unless you say otherwise. A loose mock returns default values for calls you never set
up, which can hide a missing setup. [Moq1400](rules/Moq1400.md) asks you to choose a behavior on purpose.
[Moq1410](rules/Moq1410.md) asks for `MockBehavior.Strict`.

```csharp
public interface ISample
{
    int Calculate();
}

var mock = new Mock<ISample>();                     // Moq1400 and Moq1410
var mock2 = Mock.Of<ISample>();                     // Moq1400 and Moq1410
var mock3 = new Mock<ISample>(MockBehavior.Loose);  // Moq1410
```

```csharp
var mock = new Mock<ISample>(MockBehavior.Strict);
var mock2 = Mock.Of<ISample>(MockBehavior.Strict);
var repo = new MockRepository(MockBehavior.Strict);
```

Both rules have a code fix. Moq1400 defaults to Warning and accepts `Loose` or `Strict`. Moq1410 defaults to Info.
Turn off Moq1410 if your team accepts explicit `Loose` mocks.

## Verification hygiene

Two rules keep verification honest. [Moq1420](rules/Moq1420.md) flags `Times.AtLeastOnce()`, because that is already
the default for `Verify`, `VerifyGet`, and `VerifySet`. [Moq1500](rules/Moq1500.md) flags a local `MockRepository` that
creates mocks and never calls `Verify()`.

```csharp
var mock = new Mock<IService>();

mock.Verify(x => x.DoSomething(), Times.AtLeastOnce()); // Moq1420: redundant
mock.Verify(x => x.DoSomething());                      // Same check
mock.Verify(x => x.DoSomething(), Times.Once());        // Not redundant: exactly one call
```

Use `Times.Once()` when you need to verify exactly one call.

```csharp
[Test]
public void TestMethod()
{
    var repository = new MockRepository(MockBehavior.Strict); // Moq1500: MockRepository.Verify() should be called
    var mock = repository.Create<IMyInterface>();

    mock.Setup(x => x.DoSomething()).Returns(42);
}
```

```csharp
[Test]
public void TestMethod()
{
    var repository = new MockRepository(MockBehavior.Strict);
    var mock = repository.Create<IMyInterface>();

    mock.Setup(x => x.DoSomething()).Returns(42).Verifiable();

    mock.Object.DoSomething();

    repository.Verify();
}
```

`repository.Verify()` checks only the setups marked `Verifiable()`. `repository.VerifyAll()` checks every setup, but
Moq1500 looks for `Verify()` only and still reports a repository that calls only `VerifyAll()`.

## Protected setups without ItExpr

The string-based `Protected().Setup(...)` and `Protected().Verify(...)` overloads need `ItExpr` matchers.
[Moq1600](rules/Moq1600.md) flags `It` matchers there, because they compile and then fail at run time.

```csharp
using Moq;
using Moq.Protected;

public abstract class MyBase
{
    protected virtual bool Foo(string arg) => false;
}

var mock = new Mock<MyBase>();

mock.Protected()
    .Setup<bool>("Foo", It.IsAny<string>()) // Moq1600: use ItExpr.IsAny
    .Returns(true);
```

```csharp
var mock = new Mock<MyBase>();

mock.Protected()
    .Setup<bool>("Foo", ItExpr.IsAny<string>())
    .Returns(true);
```

The lambda-based API, `Protected().As<TInterface>()`, uses the normal `It` matchers. See [Moq1600](rules/Moq1600.md).

## Install and configure

Before you install, make sure you use a
[supported version of the .NET SDK](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

Run this command in each test project:

```shell
dotnet add package Moq.Analyzers
```

To cover every test project at once, add the package in a `Directory.Build.props` file in the folder that holds your
test projects. Replace `x.y.z` with the current version on [NuGet](https://www.nuget.org/packages/Moq.Analyzers).

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="Moq.Analyzers" Version="x.y.z" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

With central package management, omit `Version` here and set it in `Directory.Packages.props`.

MSBuild stops at the first `Directory.Build.props` it finds. If a parent folder has one too, import it from the new
file. See [Customize the build by folder](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory).

Change a rule's severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.Moq1410.severity = none
dotnet_diagnostic.Moq1400.severity = error
```

Each rule page shows how to suppress a single violation. See also
[Suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings)
on Microsoft Learn.

## FAQ

### Why does Moq throw "Invalid setup on a non-virtual member"?

Moq can only intercept `virtual`, `abstract`, or interface members. [Moq1200](rules/Moq1200.md) flags the setup at
compile time. Newer Moq versions word the error differently. See
[the runtime error this replaces](#the-runtime-error-this-replaces).

### Can Moq mock a sealed class?

No. Moq generates a subclass, and a sealed class cannot be subclassed. [Moq1000](rules/Moq1000.md) flags it.

### Should I use Returns or ReturnsAsync for async methods?

Use `ReturnsAsync`. [Moq1201](rules/Moq1201.md), [Moq1206](rules/Moq1206.md), and [Moq1208](rules/Moq1208.md) flag the
common wrong patterns. Moq1201 applies only to Moq versions older than 4.16.0.

### Should I mock ILogger with Moq?

No. Use `NullLogger` or `FakeLogger`. [Moq1004](rules/Moq1004.md) flags mocks of `ILogger` and `ILogger<T>`. To
check log output, see [Tests that verify logging](rules/Moq1004.md#tests-that-verify-logging-fakelogger).

### Why does my Moq Callback throw?

The `Callback` parameters must match the signature of the method in `Setup`. [Moq1100](rules/Moq1100.md) flags a
mismatch and offers a code fix.

### How do I install Moq.Analyzers?

Run `dotnet add package Moq.Analyzers` in each test project.
