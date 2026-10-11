# How to use Callback in Moq: parameters, return values, out and ref, and async

Use `Callback` to run code when a mocked method is called, for example to capture its arguments. The callback's
parameters must match the mocked method's parameters in number, order, and type.
[Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers) rule [Moq1100](rules/Moq1100.md) checks this at build
time.

| You want to | Use |
| ----------- | --- |
| Run code or capture arguments | `Callback((int id, ...) => ...)` |
| Return a value built from the arguments | `Returns((int id, ...) => ...)` |
| Do both | `Callback(...).Returns(...)` |
| Set an `out` or `ref` argument | A custom delegate type and `It.Ref<T>.IsAny` |
| Run code on a method that returns `Task` | `Callback(...).Returns(Task.CompletedTask)` |

The samples on this page use this interface. Each one compiles against Moq 4.20.72. Blocks marked `// Moq1100` throw
at run time. Blocks without `var mock` continue the block above.

```csharp
public interface IOrderService
{
    int Place(int id, string sku, DateTime at);
    Task SaveAsync(int id);
    Task<int> CountAsync(string sku);
    bool TryGet(int id, out string sku);
    void Rename(ref string sku);
}
```

## Contents

- [Capture arguments with Callback](#capture-arguments-with-callback)
- [Callback with multiple parameters](#callback-with-multiple-parameters)
- [`Callback<T>` generic form](#callbackt-generic-form)
- [Callback and Returns together](#callback-and-returns-together)
- [Callback vs Returns](#callback-vs-returns)
- [Out and ref parameters](#out-and-ref-parameters)
- [Async methods](#async-methods)
- [Errors you might see](#errors-you-might-see)
- [FAQ](#faq)
- [See also](#see-also)

## Capture arguments with Callback

Declare one lambda parameter for each parameter of the mocked method. Moq passes the call's arguments to the lambda.

```csharp
var mock = new Mock<IOrderService>();
var placed = new List<int>();

mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback((int id, string sku, DateTime at) => placed.Add(id))
    .Returns(1);

mock.Object.Place(4, "A1", DateTime.UtcNow); // placed now holds 4
```

Write the parameter types. An untyped lambda, `Callback((id, sku, at) => { })`, does not compile. With Moq 4.20.72
the compiler reports CS1660.

A callback with no parameters is always valid. Use it when you do not need the arguments.

```csharp
var calls = 0;

mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback(() => calls++)
    .Returns(1);
```

## Callback with multiple parameters

Order matters. List the lambda parameters in the same order as the method. The names can differ. The types cannot.

```csharp
var mock = new Mock<IOrderService>();

mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback((string sku, int id, DateTime at) => { }) // Moq1100
    .Returns(1);
```

```csharp
var mock = new Mock<IOrderService>();

mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback((int id, string sku, DateTime at) => { })
    .Returns(1);
```

A callback that takes some of the parameters is also wrong. Take all of them or none of them.

```csharp
mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback((int id, string sku) => { }) // Moq1100: 2 parameters, the method has 3
    .Returns(1);
```

## `Callback<T>` generic form

`Callback<T1, T2, T3>` states the types once, so the lambda parameters need no types. The type arguments follow the
same rule: same number, same order, same types as the method.

```csharp
var mock = new Mock<IOrderService>();

mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback<string, int, DateTime>((sku, id, at) => { }) // Moq1100
    .Returns(1);
```

```csharp
var mock = new Mock<IOrderService>();

mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback<int, string, DateTime>((id, sku, at) => { })
    .Returns(1);
```

Moq.Analyzers 0.4.2 does not report the generic form. Builds of the analyzer from `main` do.

## Callback and Returns together

Chain `Callback` and `Returns` on the same setup. The callback records the call. `Returns` supplies the result.

```csharp
var mock = new Mock<IOrderService>();
var placed = new List<int>();

mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback((int id, string sku, DateTime at) => placed.Add(id))
    .Returns((int id, string sku, DateTime at) => id * 10);

int result = mock.Object.Place(4, "A1", DateTime.UtcNow); // result is 40, placed holds 4
```

You can put a callback on each side of `Returns`. The first runs before Moq computes the return value. The second
runs after.

```csharp
var log = new List<string>();

mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Callback(() => log.Add("before"))
    .Returns(() => { log.Add("returns"); return 1; })
    .Callback(() => log.Add("after"));

mock.Object.Place(4, "A1", DateTime.UtcNow); // log holds before, returns, after
```

## Callback vs Returns

`Callback` runs code and returns nothing. `Returns` sets the value the mocked method gives back.

| | `Callback` | `Returns` |
| --- | --- | --- |
| Purpose | Side effects: capture, count, log | The result of the call |
| Lambda return type | `void` | The method's return type |
| Lambda with no parameters | Valid | Valid |
| Lambda with parameters | Match the method | Match the method |

Give a `Returns` lambda the same parameters as the method. Moq1100 checks it the same way it checks a callback.

```csharp
mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Returns((int id) => id); // Moq1100: 1 parameter, the method has 3
```

```csharp
mock.Setup(x => x.Place(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
    .Returns((int id, string sku, DateTime at) => id);
```

`Callback` alone does not set a return value. Without `Returns`, a loose mock returns the default value, and
[Moq1203](rules/Moq1203.md) flags the setup.

## Out and ref parameters

`Action` and `Func` cannot declare `out` or `ref` parameters. Declare your own delegate type that matches the method,
and match the argument with `It.Ref<T>.IsAny` in the setup. This needs Moq 4.8 or later.

```csharp
public delegate void TryGetCallback(int id, out string sku);
public delegate void RenameCallback(ref string sku);
```

```csharp
var mock = new Mock<IOrderService>();

mock.Setup(x => x.TryGet(It.IsAny<int>(), out It.Ref<string>.IsAny))
    .Callback(new TryGetCallback((int id, out string sku) => sku = "A1"))
    .Returns(true);

bool found = mock.Object.TryGet(1, out string sku); // found is true, sku is "A1"
```

```csharp
mock.Setup(x => x.Rename(ref It.Ref<string>.IsAny))
    .Callback(new RenameCallback((ref string sku) => sku = "B2"));

string name = "A1";
mock.Object.Rename(ref name); // name is "B2"
```

On C# 10 or later you can skip the delegate type. Pass a typed lambda with a block body.

```csharp
mock.Setup(x => x.TryGet(It.IsAny<int>(), out It.Ref<string>.IsAny))
    .Callback((int id, out string sku) => { sku = "A1"; })
    .Returns(true);
```

For a fixed `out` value you need no callback. Put the value in a variable and pass it in the setup.

```csharp
var mock = new Mock<IOrderService>();
string expected = "A1";

mock.Setup(x => x.TryGet(1, out expected)).Returns(true);

bool found = mock.Object.TryGet(1, out string sku); // found is true, sku is "A1"
```

To set the `out` value and the return value in one place, give `Returns` a delegate that returns the method's type.

```csharp
public delegate bool TryGetReturns(int id, out string sku);
```

```csharp
mock.Setup(x => x.TryGet(It.IsAny<int>(), out It.Ref<string>.IsAny))
    .Returns(new TryGetReturns((int id, out string sku) => { sku = "A1"; return true; }));
```

Three mistakes to avoid:

- A plain lambda without the `out` modifier, `Callback((int id, string sku) => { })`, raises Moq1100 and throws at
  run time. Use the delegate pattern above.
- A delegate that returns a value, such as `TryGetReturns`, works with `Returns` and throws when passed to
  `Callback`. `Callback` accepts `void` delegates only. Moq1100 does not report this case
  ([#1434](https://github.com/rjmurillo/moq.analyzers/issues/1434)).
- A typed lambda with an expression body and no delegate type, `Callback((int id, out string sku) => sku = "A1")`.
  It compiles. The lambda returns the assigned `string`, so it is not a `void` delegate, and Moq throws the same
  error. Moq1100 does not report it. Use a block body: `{ sku = "A1"; }`.

## Async methods

A callback on an async method is the same as on any other method. Match the parameters, then set the returned task.
For a method that returns `Task`, end with `Returns(Task.CompletedTask)`.

```csharp
var mock = new Mock<IOrderService>();
var saved = new List<int>();

mock.Setup(x => x.SaveAsync(It.IsAny<int>()))
    .Callback((int id) => saved.Add(id))
    .Returns(Task.CompletedTask);

await mock.Object.SaveAsync(7); // saved holds 7
```

For a method that returns `Task<T>`, end with `ReturnsAsync`.

```csharp
var skus = new List<string>();

mock.Setup(x => x.CountAsync(It.IsAny<string>()))
    .Callback((string sku) => skus.Add(sku))
    .ReturnsAsync(3);

int count = await mock.Object.CountAsync("A1"); // count is 3, skus holds "A1"
```

## Errors you might see

Moq checks the callback when the `Callback` or `Returns` line runs. The test fails on the setup line, before your
code calls the mocked method. Each case below throws `System.ArgumentException`. The text is from Moq 4.18.4 and
4.20.72.

A `Callback` with the wrong number, order, or type of parameters:

```text
Invalid callback. Setup on method with parameters (int, string, DateTime) cannot invoke callback with parameters (string, int).
```

A `Returns` lambda with the wrong number of parameters:

```text
Invalid callback. Setup on method with 3 parameter(s) cannot invoke callback with different number of parameters (1).
```

A delegate with a return value passed to `Callback`:

```text
Invalid callback. This overload of the "Callback" method only accepts "void" (C#) or "Sub" (VB.NET) delegates with parameter types matching those of the set up method.
```

One case fails later. A `Returns` lambda with the right number of parameters and the wrong types passes setup. For
example, `Returns((string sku, int id, DateTime at) => 1)` throws this when `Place` is called:

```text
Object of type 'System.Int32' cannot be converted to type 'System.String'.
```

Older versions, such as Moq 4.10.1, print CLR type names in the first message, as in `(Int32,String,DateTime)`.

Moq1100 reports the first, second, and fourth case at build time:

```text
warning Moq1100: Callback signature for 'Place' must match the signature of the mocked method
```

The message says "Callback" for a `Returns` lambda as well. To fix it, make the lambda parameters match the mocked
method. A code fix does this for a lambda passed to `Callback` or `Returns`. See
[How to fix](rules/Moq1100.md#how-to-fix) for its limits.

Moq1100 does not report these mismatches. Moq rejects them. They are tracked in
[#1434](https://github.com/rjmurillo/moq.analyzers/issues/1434).

- A callback parameter of a different type that C# can convert. Examples: `long` or `short` for an `int` parameter,
  or `string` for an `object` parameter. Moq throws the first message above. A base type works, such as `object` for
  an `int` parameter.
- A delegate with a return value passed to `Callback`.

## FAQ

### What does "Invalid callback. Setup on method with parameters cannot invoke callback with parameters" mean?

Your `Callback` lambda's parameters do not match the mocked method's parameters. Match the number, order, and types.
[Moq1100](rules/Moq1100.md) reports it at build time.

### Why do I get "Invalid callback" without using Callback?

A `Returns` lambda with parameters must match the method too. If it takes the wrong number of parameters, Moq throws
"Invalid callback" from the `Returns` call. Moq1100 flags `Returns` lambdas as well as callbacks.

### How do I get the arguments in a Moq callback?

Declare lambda parameters with the same types and order as the method, for example
`Callback((int id, string sku, DateTime at) => ...)`.

### What is the difference between Callback and Returns?

`Callback` runs code. `Returns` sets the result. You can chain both on one setup.

### How do I set an out parameter in Moq?

For a fixed value, pass a variable that holds the value as the `out` argument in `Setup`. To compute it, use
`It.Ref<T>.IsAny` with a custom delegate that has an `out` parameter, or a typed lambda with a block body. See [Out and ref parameters](#out-and-ref-parameters).

### Can I use Callback on an async method?

Yes. Add the callback, then end with `Returns(Task.CompletedTask)` for `Task` or `ReturnsAsync(value)` for `Task<T>`.

## See also

- [Moq1100: Callback signature must match the signature of the mocked method](rules/Moq1100.md)
- [Moq1203: Method setup should specify a return value](rules/Moq1203.md)
- [Moq1208: Returns() delegate type mismatch on async method setup](rules/Moq1208.md)
- [Callback parameters that do not match](common-moq-mistakes.md#callback-parameters-that-do-not-match) in the guide to
  all common Moq mistakes
- [Moq quickstart: callbacks](https://github.com/devlooped/moq/wiki/Quickstart#callbacks)
