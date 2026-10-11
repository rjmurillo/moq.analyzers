# Verify calls in Moq: Times, arguments, and failure messages

Use `mock.Verify(x => x.Method(args), Times.Once())` to check that a call happened once, `Times.Never()` to check that
it did not, and `It.Is<T>(predicate)` to check argument values. Verify fails with a `MockException` that names the
expected count, the actual count, and the calls that were made. The member must be virtual, abstract, or on an
interface. [Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers) reports a non-overridable one at build time
([Moq1210](rules/Moq1210.md)). To install the analyzer, see [Install Moq.Analyzers](install.md) or
[Getting started](../README.md#getting-started).

| Goal | Code | Failure text contains |
| ---- | ---- | --------------------- |
| Called at least once | `mock.Verify(x => x.Submit(It.IsAny<Order>()))` | `Expected invocation on the mock at least once, but was never performed` |
| Called exactly once | `mock.Verify(x => x.Submit(It.IsAny<Order>()), Times.Once())` | `Expected invocation on the mock once, but was 2 times` |
| Never called | `mock.Verify(x => x.Cancel(It.IsAny<int>()), Times.Never())` | `Expected invocation on the mock should never have been performed, but was 1 times` |
| Called with a value | `mock.Verify(x => x.Submit(It.Is<Order>(o => o.Id == 1)), Times.Once())` | `Expected invocation on the mock once, but was 0 times` |
| Nothing else called | `mock.VerifyNoOtherCalls()` | `This mock failed verification due to the following unverified invocations` |
| Every setup was used | `mock.VerifyAll()` | `This mock failed verification due to the following` |

The samples on this page use these types. Each sample was built with Moq 4.20.72 and .NET SDK 10.0.401 and run with
xUnit. Blocks without `var mock` or `var strict` continue the block above. The failure text was captured from those runs on Moq 4.18.4
and 4.20.72, with the same text on both, unless a section says otherwise. Older versions, such as Moq 4.10.1, word
these messages differently. The text was checked through Moq 4.20.72.

```csharp
public interface IOrderService
{
    void Submit(Order order);
    Task SaveAsync(Order order);
    bool Cancel(int id);
    string Status { get; set; }
}

public sealed record Order(int Id, decimal Total);
```

Each sample passes a `MockBehavior`, so [Moq1400](rules/Moq1400.md) does not fire. Samples that use
`MockBehavior.Loose` still get the Info-level [Moq1410](rules/Moq1410.md) note, which asks for `Strict`.

## Contents

- [Verify a method was called](#verify-a-method-was-called)
- [Verify a call count: Once, Never, Exactly, AtLeast, AtMost, Between](#verify-a-call-count-once-never-exactly-atleast-atmost-between)
- [Verify a method was never called](#verify-a-method-was-never-called)
- [Verify argument values: It.Is, It.IsAny, exact values](#verify-argument-values-itis-itisany-exact-values)
- [Verify a complex object parameter](#verify-a-complex-object-parameter)
- [Verify calls with different arguments](#verify-calls-with-different-arguments)
- [Verify async methods](#verify-async-methods)
- [Verify properties: VerifyGet and VerifySet](#verify-properties-verifyget-and-verifyset)
- [Make sure nothing else was called: VerifyNoOtherCalls](#make-sure-nothing-else-was-called-verifynoothercalls)
- [Verifiable, Verify(), and VerifyAll()](#verifiable-verify-and-verifyall)
- [Verify call order](#verify-call-order)
- [Add a failure message](#add-a-failure-message)
- [Reading a Verify failure](#reading-a-verify-failure)
- [Things Verify cannot check](#things-verify-cannot-check)
- [FAQ](#faq)
- [See also](#see-also)

## Verify a method was called

`Verify` with no `Times` argument checks that the call happened at least once.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);

mock.Object.Submit(new Order(1, 10m));

mock.Verify(x => x.Submit(It.IsAny<Order>()));
```

`Times.AtLeastOnce()` is the default. Passing it changes nothing, and [Moq1420](rules/Moq1420.md) reports it.

```csharp
mock.Verify(x => x.Submit(It.IsAny<Order>()), Times.AtLeastOnce()); // Moq1420
mock.Verify(x => x.Submit(It.IsAny<Order>()));                      // same check
```

If `Submit` was never called, and the only call was `Cancel(3)`, Verify throws:

```text
Expected invocation on the mock at least once, but was never performed: x => x.Submit(It.IsAny<Order>())

Performed invocations:

   Mock<IOrderService:1> (x):

      IOrderService.Cancel(3)
```

## Verify a call count: Once, Never, Exactly, AtLeast, AtMost, Between

Pass a `Times` value as the second argument.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);

mock.Object.Submit(new Order(1, 10m));

mock.Verify(x => x.Submit(It.IsAny<Order>()), Times.Once());
mock.Verify(x => x.Submit(It.IsAny<Order>()), Times.Exactly(1));
mock.Verify(x => x.Submit(It.IsAny<Order>()), Times.AtLeast(1));
mock.Verify(x => x.Submit(It.IsAny<Order>()), Times.AtMost(2));
mock.Verify(x => x.Submit(It.IsAny<Order>()), Times.AtMostOnce());
mock.Verify(x => x.Submit(It.IsAny<Order>()), Times.Between(1, 3, Moq.Range.Inclusive));
```

Each `Times` value has its own failure text. The test calls are in the second column.

| Times | Calls made | Failure text |
| ----- | ---------- | ------------ |
| `Times.Once()` | 2 | `Expected invocation on the mock once, but was 2 times:` |
| `Times.Never()` | 1 | `Expected invocation on the mock should never have been performed, but was 1 times:` |
| `Times.Exactly(2)` | 1 | `Expected invocation on the mock exactly 2 times, but was 1 times:` |
| `Times.AtLeast(2)` | 1 | `Expected invocation on the mock at least 2 times, but was 1 times:` |
| `Times.AtLeastOnce()` | 0 | `Expected invocation on the mock at least once, but was never performed:` |
| `Times.AtMost(2)` | 3 | `Expected invocation on the mock at most 2 times, but was 3 times:` |
| `Times.AtMostOnce()` | 2 | `Expected invocation on the mock at most once, but was 2 times:` |
| `Times.Between(1, 3, Moq.Range.Inclusive)` | 0 | `Expected invocation on the mock between 1 and 3 times (Inclusive), but was 0 times:` |
| `Times.Between(1, 3, Moq.Range.Exclusive)` | 1 | `Expected invocation on the mock between 1 and 3 times (Exclusive), but was 1 times:` |

`Between` needs a `Range` value. `Range.Inclusive` counts both ends. `Range.Exclusive` leaves both ends out, so one
call fails `Between(1, 3, Moq.Range.Exclusive)`.

Write `Moq.Range`, not `Range`. With `<ImplicitUsings>enable</ImplicitUsings>` in the project, `Range` also names
`System.Range`. The build fails with:

```text
error CS0104: 'Range' is an ambiguous reference between 'Moq.Range' and 'System.Range'
```

## Verify a method was never called

Use `Times.Never()`.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);

mock.Object.Submit(new Order(1, 10m));

mock.Verify(x => x.Cancel(It.IsAny<int>()), Times.Never());
```

If the test had called `Cancel(7)`, Verify would throw:

```text
Expected invocation on the mock should never have been performed, but was 1 times: x => x.Cancel(It.IsAny<int>())

Performed invocations:

   Mock<IOrderService:1> (x):

      IOrderService.Cancel(7)
```

`Times.Never()` checks one member. To check that the mock got no call you did not expect, on any member, use
[VerifyNoOtherCalls](#make-sure-nothing-else-was-called-verifynoothercalls).

## Verify argument values: It.Is, It.IsAny, exact values

The arguments in the Verify expression decide which calls count.

- `It.IsAny<T>()` counts every call.
- `It.Is<T>(predicate)` counts calls whose argument passes the predicate.
- An exact value counts calls whose argument equals it. `Order` is a record, so it compares by value.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);

mock.Object.Submit(new Order(1, 10m));
mock.Object.Cancel(42);

mock.Verify(x => x.Submit(It.Is<Order>(o => o.Id == 1 && o.Total > 5m)), Times.Once());
mock.Verify(x => x.Submit(new Order(1, 10m)), Times.Once());
mock.Verify(x => x.Cancel(42), Times.Once());
mock.Verify(x => x.Cancel(It.Is<int>(id => id > 40)), Times.Once());
```

When no call matches, the performed invocations show the arguments that were passed. Here the test submitted
`new Order(2, 3m)`:

```text
Expected invocation on the mock once, but was 0 times: x => x.Submit(It.Is<Order>(o => o.Id == 1 && o.Total > 5))

Performed invocations:

   Mock<IOrderService:1> (x):

      IOrderService.Submit(Order { Id = 2, Total = 3 })
```

## Verify a complex object parameter

`It.Is<T>` with a predicate works for any object. The failure shows the whole predicate, so it does not say which
property was wrong. To get a clearer failure, capture the argument with `Capture.In` and assert on it.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);
var submitted = new List<Order>();
mock.Setup(x => x.Submit(Capture.In(submitted)));

mock.Object.Submit(new Order(1, 10m));

var order = Assert.Single(submitted);
Assert.Equal(1, order.Id);
Assert.Equal(10m, order.Total);
```

Each assert names the expected and actual value of one property. To capture with `Callback` instead, see
[Capture arguments with Callback](moq-callback.md#capture-arguments-with-callback).

## Verify calls with different arguments

Write one Verify for each argument.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);

mock.Object.Cancel(1);
mock.Object.Cancel(2);

mock.Verify(x => x.Cancel(1), Times.Once());
mock.Verify(x => x.Cancel(2), Times.Once());
```

To check the values and their order in one step, capture them and assert on the list.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);
var submitted = new List<Order>();
mock.Setup(x => x.Submit(Capture.In(submitted)));

mock.Object.Submit(new Order(1, 10m));
mock.Object.Submit(new Order(2, 20m));

Assert.Equal(new[] { 1, 2 }, submitted.Select(o => o.Id));
```

## Verify async methods

Verify an async method the same way as any other method. Await the call in the test. Do not put `await` in the
Verify expression.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);

await mock.Object.SaveAsync(new Order(2, 3m));

mock.Verify(x => x.SaveAsync(It.Is<Order>(o => o.Id == 2)), Times.Once());
```

A loose mock returns a completed task, so the `await` finishes. If `SaveAsync` was never called,
`mock.Verify(x => x.SaveAsync(It.IsAny<Order>()), Times.Once())` throws:

```text
Expected invocation on the mock once, but was 0 times: x => x.SaveAsync(It.IsAny<Order>())

Performed invocations:

   Mock<IOrderService:1> (x):
   No invocations performed.
```

To set up what an async method returns, see [How to set up async methods in Moq](moq-returnsasync.md).

## Verify properties: VerifyGet and VerifySet

`VerifyGet` checks reads. `VerifySet` checks assignments.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);

_ = mock.Object.Status;
mock.Object.Status = "Shipped";

mock.VerifyGet(x => x.Status, Times.Once());
mock.VerifySet(x => x.Status = "Shipped", Times.Once());
```

For strict mocks, matching any value, and the obsolete overloads, see
[Match or verify assignments: SetupSet and VerifySet](moq-property-setup.md#match-or-verify-assignments-setupset-and-verifyset).

## Make sure nothing else was called: VerifyNoOtherCalls

`VerifyNoOtherCalls` fails if the mock got a call that no earlier verification covered.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);

mock.Object.Submit(new Order(1, 10m));

mock.Verify(x => x.Submit(It.IsAny<Order>()));
mock.VerifyNoOtherCalls();
```

If the test also called `Cancel(1)`, `VerifyNoOtherCalls` throws:

```text
Mock<IOrderService:1>:
This mock failed verification due to the following unverified invocations:

   IOrderService.Cancel(1)
```

These runs show which calls count as verified:

| Before `VerifyNoOtherCalls` | Result |
| --------------------------- | ------ |
| `mock.Verify(expression)` matched the call | Verified |
| The call matched a `Verifiable()` setup, then `mock.Verify()` ran | Verified |
| The call matched any setup, then `mock.VerifyAll()` ran | Verified |
| The call matched a setup that was not `Verifiable()`, then `mock.Verify()` ran | Not verified |
| The call matched a setup, and nothing verified it | Not verified |
| The mock got no calls | Passes |

A setup alone does not verify a call.

## Verifiable, Verify(), and VerifyAll()

`Verifiable()` marks a setup. `mock.Verify()` with no arguments checks only the marked setups. `mock.VerifyAll()`
checks every setup.

```csharp
var strict = new Mock<IOrderService>(MockBehavior.Strict);
strict.Setup(x => x.Cancel(1)).Returns(true).Verifiable();

strict.Object.Cancel(1);

strict.Verify(); // checks setups marked Verifiable
```

A setup without `Verifiable()` that never ran passes `mock.Verify()` and fails `mock.VerifyAll()`. A `Verifiable()`
setup that never ran fails both, with the same text:

```text
Mock<IOrderService:1>:
This mock failed verification due to the following:

   IOrderService x => x.Cancel(1):
   This setup was not matched.
```

Moq 4.20.72 has `Verifiable(Times)` and `Verifiable(Times, string)`. Moq 4.18.4 does not. The count is
checked in two places:

- Too few calls: `mock.Verify()` and `mock.VerifyAll()` fail. With `Verifiable(Times.Exactly(2))` and one call, the
  text is `Expected invocation on the mock exactly 2 times, but was 1 times: x => x.Cancel(1)`.
- Too many calls: the call that goes over the limit throws. With `Verifiable(Times.Once())`, the second call throws
  `Expected invocation on the mock once, but was 2 times: x => x.Cancel(1)`.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Strict);
mock.Setup(x => x.Cancel(1)).Returns(true).Verifiable(Times.Exactly(2));

mock.Object.Cancel(1);
mock.Object.Cancel(1);

mock.Verify();
```

For a `MockRepository`, the same split holds: `repository.Verify()` checks the `Verifiable()` setups of every mock it
created. With two mocks, an unmatched `Verifiable()` setup on the second one fails `repository.Verify()`. [Moq1500](rules/Moq1500.md) reports a repository that never calls `Verify()`. See
[Verification hygiene](common-moq-mistakes.md#verification-hygiene).

## Verify call order

`Verify` checks counts, not order. To check order, set up a strict mock with a `MockSequence`. Each `InSequence`
setup matches only after the one before it ran.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Strict);
var sequence = new MockSequence();
mock.InSequence(sequence).Setup(x => x.Submit(It.IsAny<Order>()));
mock.InSequence(sequence).Setup(x => x.SaveAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

var order = new Order(1, 10m);
mock.Object.Submit(order);
await mock.Object.SaveAsync(order);
```

A call out of order has no matching setup, so the strict mock throws on that call. Calling `SaveAsync` first
throws:

```text
IOrderService.SaveAsync(Order { Id = 1, Total = 10 }) invocation failed with mock behavior Strict.
All invocations on the mock must have a corresponding setup.
```

Know the limits before you rely on it. These runs used `Cancel(1)` and then `Cancel(2)` as the sequence:

- On a loose mock, an out-of-order call does not throw. It gets the default value. `Cancel(2)` called first returned
  `false`, not the `true` from its setup.
- After a step runs, the sequence moves on. A second `Cancel(1)` on a strict mock throws the same strict-mock error.
- `VerifyAll()` does not report a later step that never ran. The test passed with only `Cancel(1)` called.

## Add a failure message

Pass a message as the last argument. Moq puts it on the first line of the failure.

```csharp
var mock = new Mock<IOrderService>(MockBehavior.Loose);

mock.Verify(x => x.Submit(It.IsAny<Order>()), Times.Once(), "Checkout should submit the order");
```

```text
Checkout should submit the order
Expected invocation on the mock once, but was 0 times: x => x.Submit(It.IsAny<Order>())

Performed invocations:

   Mock<IOrderService:1> (x):
   No invocations performed.
```

`Verify(expression, "message")` without `Times` also works. It checks for at least one call.

## Reading a Verify failure

Here is the full message from `Times.Once()` after two calls to `Submit`:

```text

Expected invocation on the mock once, but was 2 times: x => x.Submit(It.IsAny<Order>())

Performed invocations:

   Mock<IOrderService:1> (x):

      IOrderService.Submit(Order { Id = 1, Total = 10 })
      IOrderService.Submit(Order { Id = 2, Total = 20 })
```

Read it line by line:

1. The first line holds your failure message. With no message, it is empty. In `dotnet test` output with xUnit, the
   line reads `Moq.MockException :` and the text starts on the next line. The other blocks on this page leave the
   empty line out.
2. The expected count, the actual count, and the Verify expression.
3. `Performed invocations:` starts the list of every call the mock got, matched or not.
4. The mock's name and the lambda parameter, here `Mock<IOrderService:1> (x):`.
5. One line per call, with its arguments. A record prints its properties, which shows what was passed. With no
   calls, the list says `No invocations performed.`

Compare the arguments in the list with the Verify expression. Most failures are a wrong argument, not a missing call.

## Things Verify cannot check

Moq can verify only calls it can intercept.

**Non-virtual members.** Moq throws `NotSupportedException`. [Moq1210](rules/Moq1210.md) reports it at build time.

```csharp
public class PriceCalculator
{
    public decimal Total(Order order) => order.Total;
}

var mock = new Mock<PriceCalculator>(MockBehavior.Loose);
mock.Verify(x => x.Total(It.IsAny<Order>())); // Moq1210
```

```text
Unsupported expression: x => x.Total(It.IsAny<Order>())
Non-overridable members (here: PriceCalculator.Total) may not be used in setup / verification expressions.
```

For older Moq wording, see [Same error, different API](rules/Moq1210.md#same-error-different-api). For fixes, see
[Setting up or verifying a non-virtual member](common-moq-mistakes.md#setting-up-or-verifying-a-non-virtual-member).

**Extension methods.** An extension method is a static method, so Moq cannot intercept it. Verify the interface
method it calls instead. For an extension method `SubmitAll(this IOrderService service, IEnumerable<Order> orders)`,
Moq throws `NotSupportedException`:

```text
Unsupported expression: x => x.SubmitAll(It.IsAny<IEnumerable<Order>>())
Extension methods (here: OrderServiceExtensions.SubmitAll) may not be used in setup / verification expressions.
```

Builds of the analyzer from `main` report this as Moq1210. Moq.Analyzers 0.4.2 does not.

**ILogger extension methods.** `LogInformation` and the other `Log*` methods are extension methods. Use `FakeLogger`
instead of a mock. See [Tests that verify logging: FakeLogger](rules/Moq1004.md#tests-that-verify-logging-fakelogger)
and [Mocking ILogger](common-moq-mistakes.md#mocking-ilogger). Rule [Moq1004](rules/Moq1004.md) ships after
Moq.Analyzers 0.4.2.

## FAQ

### How do I verify a method was called once in Moq?

Use `mock.Verify(x => x.Method(), Times.Once())`. It fails on zero calls and on two or more.

### How do I verify a method was not called?

Use `Times.Never()`. To check that the mock got no call you did not verify, on any member, call
`mock.VerifyNoOtherCalls()`.

### How do I verify a parameter value?

Use `It.Is<T>(x => ...)` in the Verify expression. For a clearer failure, capture the argument with `Capture.In` and
assert on it.

### What is the difference between Verify, VerifyAll, and Verifiable?

`mock.Verify(expression)` checks one call. `Verifiable()` marks a setup, and `mock.Verify()` with no arguments checks
the marked setups. `mock.VerifyAll()` checks every setup.

### How do I verify an async method was called?

Verify it like a sync method, for example `mock.Verify(x => x.SaveAsync(It.IsAny<Order>()), Times.Once())`. Do not
put `await` in the expression.

### Why does Verify fail with "Invalid verify on a non-virtual member"?

The member is not virtual, abstract, or on an interface, so Moq cannot intercept it. Moq 4.2 through 4.10.1 use
that text. Moq 4.18.4 and 4.20.72 throw `Non-overridable members (here: ...) may not be used in setup / verification
expressions.` [Moq1210](rules/Moq1210.md) reports it at build time.

### Is `Times.AtLeastOnce()` needed?

No. It is the default for `Verify`. [Moq1420](rules/Moq1420.md) flags it.

## See also

- [Moq1210: Verify should be used only for overridable members](rules/Moq1210.md)
- [Moq1420: Redundant Times.AtLeastOnce() specification can be removed](rules/Moq1420.md)
- [Moq1500: MockRepository.Verify() should be called](rules/Moq1500.md)
- [Verification hygiene](common-moq-mistakes.md#verification-hygiene) in the guide to all common Moq mistakes
- [Mocking properties in Moq](moq-property-setup.md)
- [How to use Callback in Moq](moq-callback.md)
- [Moq quickstart: verification](https://github.com/devlooped/moq/wiki/Quickstart#verification)
