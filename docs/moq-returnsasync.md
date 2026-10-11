# How to set up async methods in Moq: ReturnsAsync, Returns, and .Result

Use `ReturnsAsync(value)` for methods that return `Task<T>` or `ValueTask<T>`. Use `Returns(Task.CompletedTask)` for
methods that return `Task`. [Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers) flags three common wrong
patterns: [Moq1201](rules/Moq1201.md), [Moq1206](rules/Moq1206.md), and [Moq1208](rules/Moq1208.md).

| The method returns | Set it up with                     |
| ------------------ | ---------------------------------- |
| `Task<T>`          | `ReturnsAsync(value)`              |
| `ValueTask<T>`     | `ReturnsAsync(value)`              |
| `Task`             | `Returns(Task.CompletedTask)`      |
| `ValueTask`        | `Returns(ValueTask.CompletedTask)` |

The samples on this page use this interface. Each sample was built with Moq 4.20.72 and .NET SDK 10.0.401. Samples
marked with an error code or a rule ID fail as shown.

```csharp
public record Customer(int Id);

public interface ICustomerStore
{
    Task<Customer?> FindAsync(int id);
    ValueTask<int> CountAsync();
    Task SaveAsync(Customer customer);
    ValueTask FlushAsync();
}
```

## Contents

- [ReturnsAsync with a value](#returnsasync-with-a-value)
- [ReturnsAsync with parameters](#returnsasync-with-parameters)
- [ReturnsAsync(null)](#returnsasyncnull)
- [Methods that return Task or ValueTask (no value)](#methods-that-return-task-or-valuetask-no-value)
- [Returns vs ReturnsAsync](#returns-vs-returnsasync)
- [Three mistakes Moq.Analyzers catches](#three-mistakes-moqanalyzers-catches)
- [Throwing from async methods](#throwing-from-async-methods)
- [FAQ](#faq)
- [See also](#see-also)

## ReturnsAsync with a value

Pass the result value. Moq wraps it in a completed `Task<T>` or `ValueTask<T>` for you.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(1)).ReturnsAsync(new Customer(1)); // Task<Customer?>
mock.Setup(x => x.CountAsync()).ReturnsAsync(3);               // ValueTask<int>

Customer? customer = await mock.Object.FindAsync(1);
int count = await mock.Object.CountAsync();
```

To compute the value on each call, pass a lambda. Moq runs the lambda every time the method is called.

```csharp
var mock = new Mock<ICustomerStore>();
int calls = 0;

mock.Setup(x => x.CountAsync()).ReturnsAsync(() => ++calls);

int first = await mock.Object.CountAsync();  // 1
int second = await mock.Object.CountAsync(); // 2
```

## ReturnsAsync with parameters

To build the result from the arguments of the call, pass a lambda with typed parameters. The parameter types must
match the parameters of the method you set up.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Customer(id));

Customer? customer = await mock.Object.FindAsync(7); // Customer { Id = 7 }
```

## ReturnsAsync(null)

A bare `null` does not compile. It fits both the `ReturnsAsync` overload that takes a value and the overload that
takes a function.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(42)).ReturnsAsync(null); // CS0121
```

The compiler reports this error:

```text
error CS0121: The call is ambiguous between the following methods or properties: 'Moq.ReturnsExtensions.ReturnsAsync<TMock, TResult>(Moq.Language.IReturns<TMock, System.Threading.Tasks.Task<TResult>>, TResult)' and 'Moq.ReturnsExtensions.ReturnsAsync<TMock, TResult>(Moq.Language.IReturns<TMock, System.Threading.Tasks.Task<TResult>>, System.Func<TResult>)'
```

Give the `null` a type. Either line works.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(42)).ReturnsAsync((Customer?)null);
mock.Setup(x => x.FindAsync(43)).ReturnsAsync(default(Customer));

Customer? customer = await mock.Object.FindAsync(42); // null
```

If your project does not use nullable reference types, write `(Customer)null`.

## Methods that return Task or ValueTask (no value)

`ReturnsAsync` needs a result value, so it does not apply to a method that returns `Task` or `ValueTask`. Return a
completed task with `Returns`.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.SaveAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);
mock.Setup(x => x.FlushAsync()).Returns(ValueTask.CompletedTask);

await mock.Object.SaveAsync(new Customer(1));
await mock.Object.FlushAsync();
```

`ValueTask.CompletedTask` exists in .NET 5 and later. On .NET Framework and .NET Core 3.1, use `default(ValueTask)`.

Moq has no `ReturnsAsync` overload that takes no arguments. This call does not compile:

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.SaveAsync(It.IsAny<Customer>())).ReturnsAsync(); // CS1501
```

```text
error CS1501: No overload for method 'ReturnsAsync' takes 0 arguments
```

## Returns vs ReturnsAsync

`ReturnsAsync` takes the result value and wraps it. `Returns` takes the task itself. These pairs do the same thing:

| With `ReturnsAsync`                            | With `Returns`                         |
| ---------------------------------------------- | -------------------------------------- |
| `ReturnsAsync(3)` on a `Task<int>` method      | `Returns(() => Task.FromResult(3))`    |
| `ReturnsAsync(3)` on a `ValueTask<int>` method | `Returns(() => new ValueTask<int>(3))` |

Moq's [`ReturnsExtensions.cs`](https://github.com/devlooped/moq/blob/v4.20.72/src/Moq/ReturnsExtensions.cs) shows
the wrapping. Use `ReturnsAsync` when you have a value. Use `Returns` when you already hold a task, such as one from a
`TaskCompletionSource`.

```csharp
var mock = new Mock<ICustomerStore>();
var pending = new TaskCompletionSource<Customer?>();

mock.Setup(x => x.FindAsync(1)).Returns(pending.Task);
mock.Setup(x => x.CountAsync()).Returns(() => new ValueTask<int>(3));

Task<Customer?> find = mock.Object.FindAsync(1); // not completed yet
pending.SetResult(new Customer(1));
Customer? customer = await find;
```

## Three mistakes Moq.Analyzers catches

Install the analyzer to get these at build time. Moq1208 needs a version newer than 0.4.2.

```shell
dotnet add package Moq.Analyzers
```

| Wrong pattern                                         | Rule                        | Default severity | Code fix |
| ----------------------------------------------------- | --------------------------- | ---------------- | -------- |
| `Returns` with a delegate that returns the bare value | [Moq1208](rules/Moq1208.md) | Warning          | Yes      |
| `Returns` with an `async` lambda                      | [Moq1206](rules/Moq1206.md) | Warning          | No       |
| `.Result` inside `Setup` (Moq older than 4.16.0)      | [Moq1201](rules/Moq1201.md) | Error            | No       |

### Returns with a delegate that returns the bare value (Moq1208)

Moq1208 is not in Moq.Analyzers 0.4.2. It ships in the next release.

The delegate returns `int`, but `CountAsync` returns `ValueTask<int>`.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.CountAsync()).Returns(() => 3); // Moq1208
```

```text
warning Moq1208: Returns() delegate for async method 'CountAsync' should return 'ValueTask<int>', not 'int'. Use ReturnsAsync() or wrap with Task.FromResult().
```

With C# 10 or later, this line compiles. Without the analyzer, you find the mistake when the test runs. Moq throws
`System.ArgumentException` as soon as `Returns` runs. Checked at Moq 4.18.4 and 4.20.72:

```text
Invalid callback. Setup on method with return type 'ValueTask<int>' cannot invoke callback with return type 'int'.
```

Moq 4.8.2, 4.15.2, and 4.16.0 throw the same exception type and print the type names differently:

```text
Invalid callback. Setup on method with return type (System.Threading.Tasks.ValueTask`1[System.Int32]) cannot invoke callback with return type (System.Int32).
```

Fix it with `ReturnsAsync`. The Moq1208 code fix renames `Returns` to `ReturnsAsync` and keeps the lambda. You can
then shorten `ReturnsAsync(() => 3)` to `ReturnsAsync(3)`.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.CountAsync()).ReturnsAsync(3);
```

### Returns with an async lambda (Moq1206)

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(1)).Returns(async () => new Customer(1)); // Moq1206
```

```text
warning Moq1206: Async method 'FindAsync' setups should use ReturnsAsync instead of Returns with async lambda
```

This setup runs. `ReturnsAsync` does the same job here with less code. The compiler in .NET SDK 8.0.425 also reports
CS1998 here, because the lambda is `async` and never awaits. The compiler in .NET SDK 10.0.401 does not.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(1)).ReturnsAsync(new Customer(1));
```

### .Result inside Setup (Moq1201)

Moq1201 reports only when your project references a Moq version older than 4.16.0.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(1).Result).Returns(new Customer(1)); // Moq1201 on Moq older than 4.16.0
```

```text
error Moq1201: Setup of async method 'FindAsync' should use ReturnsAsync instead of .Result
```

Older Moq versions cannot set up `.Result`. Moq 4.15.2 throws `System.NotSupportedException` when `Setup` runs:

```text
Unsupported expression: ... => ....Result
Non-overridable members (here: Task<Customer>.get_Result) may not be used in setup / verification expressions.
```

Set up the method and use `ReturnsAsync`. This works on every Moq version checked for this page, from 4.8.2 to
4.20.72.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(1)).ReturnsAsync(new Customer(1));
```

Moq 4.16.0 added support for `.Result` in setup expressions. See the
[Moq 4.16.0 changelog](https://github.com/devlooped/moq/blob/v4.18.4/CHANGELOG.md#4160-2021-01-16). On Moq 4.16.0
and later, the `.Result` setup above works, and the analyzer does not report Moq1201.

## Throwing from async methods

Use `ThrowsAsync` to make an async method fail. The call returns a faulted task. The exception surfaces when you
await it.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(1)).ThrowsAsync(new InvalidOperationException("Store is offline."));
mock.Setup(x => x.SaveAsync(It.IsAny<Customer>())).ThrowsAsync(new InvalidOperationException("Store is offline."));
mock.Setup(x => x.CountAsync()).ThrowsAsync(new InvalidOperationException("Store is offline."));
mock.Setup(x => x.FlushAsync()).ThrowsAsync(new InvalidOperationException("Store is offline."));
```

Moq 4.20.72 has `ThrowsAsync` for all four return types. Moq 4.18.4 has no `ThrowsAsync` for a method that returns
`ValueTask`, and the last line above fails with CS1929.

## FAQ

### What is the difference between Returns and ReturnsAsync in Moq?

`ReturnsAsync` takes the result value and wraps it in a completed `Task<T>` or `ValueTask<T>`. `Returns` needs you to
supply the task yourself.

### How do I return null from ReturnsAsync?

Cast the `null` to the result type, for example `ReturnsAsync((Customer?)null)`. A bare `null` matches more than one
overload, and the compiler reports CS0121.

### How do I use ReturnsAsync with parameters?

Pass a lambda whose parameters match the method, for example `ReturnsAsync((int id) => new Customer(id))`.

### How do I set up a method that returns Task with no value?

Use `Returns(Task.CompletedTask)`. Moq has no `ReturnsAsync` overload that takes no arguments.

### Does ReturnsAsync work with ValueTask?

Yes, for `ValueTask<T>`. For a method that returns `ValueTask` with no value, use
`Returns(ValueTask.CompletedTask)`.

### Why does `Returns(() => 42)` fail on a `Task<int>` method?

The delegate returns `int`, not `Task<int>`. Moq throws `ArgumentException` with a message that starts with
"Invalid callback. Setup on method with return type". [Moq1208](rules/Moq1208.md) flags it at build time.

## See also

- [Moq1201](rules/Moq1201.md): Setup of async methods should use `.ReturnsAsync` instance instead of `.Result`
- [Moq1206](rules/Moq1206.md): Async method setups should use ReturnsAsync instead of Returns with async lambda
- [Moq1208](rules/Moq1208.md): Returns() delegate type mismatch on async method setup
- [Async setups done wrong](common-moq-mistakes.md#async-setups-done-wrong) in the guide to all common Moq mistakes
