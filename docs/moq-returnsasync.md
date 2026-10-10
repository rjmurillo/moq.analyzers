# How to set up async methods in Moq: ReturnsAsync, Returns, and .Result

Use `ReturnsAsync(value)` for methods that return `Task<T>` or `ValueTask<T>`. Use `Returns(Task.CompletedTask)` for
methods that return `Task`. [Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers) flags three common wrong
patterns: [Moq1201](rules/Moq1201.md), [Moq1206](rules/Moq1206.md), and [Moq1208](rules/Moq1208.md).

| Method returns | Set it up with |
| -------------- | -------------- |
| `Task<T>` | `ReturnsAsync(value)` |
| `ValueTask<T>` | `ReturnsAsync(value)` |
| `Task` | `Returns(Task.CompletedTask)` |
| `ValueTask` | `Returns(ValueTask.CompletedTask)` |

The samples on this page use this record and interface. They were compiled and run against Moq 4.20.72.

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
```

To compute the value on each call, pass a lambda. Moq runs the lambda every time the method is called.

```csharp
var count = 0;
mock.Setup(x => x.CountAsync()).ReturnsAsync(() => ++count); // 1, then 2, then 3
```

## ReturnsAsync with parameters

To build the result from the call's arguments, pass a lambda whose parameters match the mocked method.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Customer(id));

Customer? customer = await mock.Object.FindAsync(7); // Customer { Id = 7 }
```

Write the parameter types in the lambda, as in `(int id)`. Without the type, `ReturnsAsync(id => new Customer(id))`
does not compile. The compiler reports CS1660.

## ReturnsAsync(null)

A bare `null` does not compile. Two `ReturnsAsync` overloads fit `null`. One takes a value and one takes a `Func`.
The compiler cannot choose.

```csharp
mock.Setup(x => x.FindAsync(42)).ReturnsAsync(null); // CS0121
```

```text
error CS0121: The call is ambiguous between the following methods or properties: 'Moq.ReturnsExtensions.ReturnsAsync<TMock, TResult>(Moq.Language.IReturns<TMock, System.Threading.Tasks.Task<TResult>>, TResult)' and 'Moq.ReturnsExtensions.ReturnsAsync<TMock, TResult>(Moq.Language.IReturns<TMock, System.Threading.Tasks.Task<TResult>>, System.Func<TResult>)'
```

Give the `null` a type. Either line below works.

```csharp
mock.Setup(x => x.FindAsync(42)).ReturnsAsync((Customer?)null);
mock.Setup(x => x.FindAsync(42)).ReturnsAsync(default(Customer));
```

## Methods that return Task or ValueTask (no value)

`ReturnsAsync` needs a value to wrap. A method that returns `Task` or `ValueTask` has no value, so use `Returns` with
a completed task.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.SaveAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);
mock.Setup(x => x.FlushAsync()).Returns(ValueTask.CompletedTask);
```

If your target framework has no `ValueTask.CompletedTask`, use `Returns(default(ValueTask))`.

Moq has no `ReturnsAsync()` overload without arguments. This line does not compile:

```csharp
mock.Setup(x => x.SaveAsync(It.IsAny<Customer>())).ReturnsAsync(); // CS1501
```

```text
error CS1501: No overload for method 'ReturnsAsync' takes 0 arguments
```

## Returns vs ReturnsAsync

`ReturnsAsync` takes the result value and wraps it. `Returns` takes the task itself, so you wrap the value.
Each pair below does the same thing.

```csharp
var mock = new Mock<ICustomerStore>();

// Task<Customer?>
mock.Setup(x => x.FindAsync(1)).ReturnsAsync(new Customer(1));
mock.Setup(x => x.FindAsync(1)).Returns(() => Task.FromResult<Customer?>(new Customer(1)));

// ValueTask<int>
mock.Setup(x => x.CountAsync()).ReturnsAsync(3);
mock.Setup(x => x.CountAsync()).Returns(() => new ValueTask<int>(3));
```

Prefer `ReturnsAsync`. It is shorter.

## Three mistakes Moq.Analyzers catches

### Moq1208: Returns with a delegate that returns the bare value

Moq1208 is not in Moq.Analyzers 0.4.2. It ships in the next release.

Before:

```csharp
var mock = new Mock<ICustomerStore>();
mock.Setup(x => x.CountAsync()).Returns(() => 3); // Moq1208
```

```text
warning Moq1208: Returns() delegate for async method 'CountAsync' should return 'ValueTask<int>', not 'int'. Use ReturnsAsync() or wrap with Task.FromResult().
```

This line compiles with C# 10 and later. Without the analyzer, the `Returns` call then throws `ArgumentException` when
the test runs. Moq 4.18.4 and 4.20.72 produce this message:

```text
System.ArgumentException: Invalid callback. Setup on method with return type 'ValueTask<int>' cannot invoke callback with return type 'int'.
```

After:

```csharp
mock.Setup(x => x.CountAsync()).ReturnsAsync(3);
```

Moq1208 has a code fix that rewrites `Returns` to `ReturnsAsync`. See [Moq1208](rules/Moq1208.md).

### Moq1206: Returns with an async lambda

Before:

```csharp
mock.Setup(x => x.FindAsync(1)).Returns(async () => new Customer(1)); // Moq1206
```

```text
warning Moq1206: Async method 'FindAsync' setups should use ReturnsAsync instead of Returns with async lambda
```

After:

```csharp
mock.Setup(x => x.FindAsync(1)).ReturnsAsync(new Customer(1));
```

The Before line runs and returns the customer. `ReturnsAsync` does the same job without the `async` lambda.
Moq1206 has no code fix. See [Moq1206](rules/Moq1206.md).

### Moq1201: .Result inside the Setup expression

Moq1201 reports only when your project references a Moq version older than 4.16.0.

Before:

```csharp
mock.Setup(x => x.FindAsync(1).Result).Returns(new Customer(1)); // Moq1201 on Moq older than 4.16.0
```

```text
error Moq1201: Setup of async method 'FindAsync' should use ReturnsAsync instead of .Result
```

Without the analyzer, Moq 4.15.2 throws `NotSupportedException` from the `Setup` call when the test runs.

After:

```csharp
mock.Setup(x => x.FindAsync(1)).ReturnsAsync(new Customer(1));
```

Moq 4.16.0 added support for `.Result` in setup expressions. See the
[Moq 4.16.0 changelog](https://github.com/devlooped/moq/blob/v4.18.4/CHANGELOG.md#4160-2021-01-16). On Moq 4.16.0 and
later the Before line works, and the analyzer stays silent. Moq1201 has no code fix. See [Moq1201](rules/Moq1201.md).

## Throwing from async methods

Use `ThrowsAsync` to make the returned task fail. The call does not throw. The task it returns is faulted, and the
exception surfaces when your code awaits it.

```csharp
var mock = new Mock<ICustomerStore>();

mock.Setup(x => x.FindAsync(1)).ThrowsAsync(new InvalidOperationException("store offline"));
mock.Setup(x => x.SaveAsync(It.IsAny<Customer>())).ThrowsAsync(new InvalidOperationException("store offline"));
mock.Setup(x => x.CountAsync()).ThrowsAsync(new InvalidOperationException("store offline"));
mock.Setup(x => x.FlushAsync()).ThrowsAsync(new InvalidOperationException("store offline"));
```

The `FlushAsync` line needs Moq 4.20 or later. Moq 4.18.4 has no `ThrowsAsync` for a `ValueTask` method with no
value.

## FAQ

### What is the difference between Returns and ReturnsAsync in Moq?

`ReturnsAsync` takes the result value and wraps it in a completed `Task<T>` or `ValueTask<T>`. `Returns` needs you to
supply the task yourself.

### How do I return null from ReturnsAsync?

Cast the `null` to the result type, for example `ReturnsAsync((Customer?)null)`. A bare `null` matches more than one
overload, so the compiler reports CS0121.

### How do I use ReturnsAsync with parameters?

Pass a lambda whose parameters match the mocked method, for example `ReturnsAsync((int id) => new Customer(id))`.

### How do I set up a method that returns Task with no value?

Use `Returns(Task.CompletedTask)`. Moq has no `ReturnsAsync()` overload without arguments.

### Does ReturnsAsync work with ValueTask?

Yes, for `ValueTask<T>`. For a `ValueTask` method with no value, use `Returns(ValueTask.CompletedTask)`.

### Why does Returns(() => 42) fail on a Task&lt;int&gt; method?

The delegate returns `int`, not `Task<int>`. Moq throws `ArgumentException` with "Invalid callback. Setup on method
with return type ..." when the test runs. [Moq1208](rules/Moq1208.md) flags it at build time.

## See also

- [Moq1201](rules/Moq1201.md): `.Result` inside a `Setup` expression
- [Moq1206](rules/Moq1206.md): `Returns` with an `async` lambda
- [Moq1208](rules/Moq1208.md): `Returns` delegate that returns the bare value
- [Async setups done wrong](common-moq-mistakes.md#async-setups-done-wrong) in the guide to all common Moq mistakes
