# Mocking properties in Moq: SetupGet, SetupSet, SetupProperty, and Setup

Use `SetupGet` (or `Setup`) to return a fixed value from a property getter. Use `SetupProperty` when the property
should remember values your code assigns. Use `SetupSet` to match assignments and `VerifySet` to verify them.
`SetupGet`, `SetupSet`, and `SetupProperty` only work on properties.
[Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers) rule [Moq1101](rules/Moq1101.md) flags them on methods.

| You want to | Use | Example |
| ----------- | --- | ------- |
| Return a fixed value from a getter | `SetupGet` or `Setup` | `mock.SetupGet(x => x.Id).Returns(7);` |
| Make one property remember assigned values | `SetupProperty` | `mock.SetupProperty(x => x.Name, "Ada");` |
| Make every property remember assigned values | `SetupAllProperties` | `mock.SetupAllProperties();` |
| Allow or react to an assignment | `SetupSet` | `mock.SetupSet(x => x.Name = "Grace");` |
| Check that your code assigned a value | `VerifySet` | `mock.VerifySet(x => x.Name = "Grace", Times.Once());` |
| Set up a method | `Setup` | `mock.Setup(x => x.GetDisplayName()).Returns("Ada");` |

The samples on this page use this interface. Each sample was built with Moq 4.20.72 and .NET SDK 10.0.401. Samples
marked with an error code or a rule ID fail as shown. The behavior notes were run on Moq 4.18.4 and 4.20.72, with the
same results on both.

```csharp
public interface ICustomer
{
    string Name { get; set; }
    int Id { get; }
    string GetDisplayName();
}
```

## Contents

- [Return a value from a property: SetupGet or Setup](#return-a-value-from-a-property-setupget-or-setup)
- [Make a property remember assignments: SetupProperty and SetupAllProperties](#make-a-property-remember-assignments-setupproperty-and-setupallproperties)
- [Match or verify assignments: SetupSet and VerifySet](#match-or-verify-assignments-setupset-and-verifyset)
- [Read-only properties and properties without a setter](#read-only-properties-and-properties-without-a-setter)
- [Indexers](#indexers)
- [Property setups on a method (Moq1101)](#property-setups-on-a-method-moq1101)
- [Properties must be virtual or on an interface](#properties-must-be-virtual-or-on-an-interface)
- [FAQ](#faq)
- [See also](#see-also)

## Return a value from a property: SetupGet or Setup

`SetupGet` sets the value a property getter returns.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
mock.SetupGet(x => x.Id).Returns(7);

int id = mock.Object.Id; // 7
```

`Setup` does the same thing when the expression is a property getter.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
mock.Setup(x => x.Name).Returns("Ada");

string name = mock.Object.Name; // "Ada"
```

The two calls are interchangeable for a getter. `Verify` and `VerifyGet` each accept a read made through either
setup. One of Moq's own error messages lists both calls and marks `SetupGet` as `//equivalent to previous one`.
`SetupGet` states your intent, and Moq1101 warns you if the member is a method.

The value is fixed. An assignment does not change what the getter returns.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Loose);
mock.SetupGet(x => x.Name).Returns("Ada");

mock.Object.Name = "Grace";
string name = mock.Object.Name; // still "Ada"
```

On a `MockBehavior.Strict` mock the same assignment throws `MockException`, because the setter has no setup:

```text
ICustomer.Name = "Grace" invocation failed with mock behavior Strict.
All invocations on the mock must have a corresponding setup.
```

To compute the value at read time, pass a lambda. Moq runs it when your code reads the property.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
var nextId = 1;
mock.SetupGet(x => x.Id).Returns(() => nextId);

nextId = 2;
int id = mock.Object.Id; // 2
```

## Make a property remember assignments: SetupProperty and SetupAllProperties

`SetupProperty` makes the property store its value. It keeps what your code assigns and returns it on the next read.
The second argument is the starting value.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
mock.SetupProperty(x => x.Name, "Ada");

mock.Object.Name = "Grace";
string name = mock.Object.Name; // "Grace"
```

Without the second argument the property starts at its default value, `null` for a `string`.

A mock does not remember assignments on its own. On a `MockBehavior.Loose` mock with no setup, the assignment is
accepted and the getter still returns the default value. On a `MockBehavior.Strict` mock the assignment throws.

`SetupProperty` needs a getter and a setter. See
[Read-only properties and properties without a setter](#read-only-properties-and-properties-without-a-setter).

### SetupProperty and SetupGet on the same property

The last setup wins.

| Order | Result after `mock.Object.Name = "Grace"` |
| ----- | ----------------------------------------- |
| `SetupProperty`, then `SetupGet(...).Returns("Fixed")` | The getter returns `"Fixed"` |
| `SetupGet(...).Returns("Fixed")`, then `SetupProperty` | The getter returns `"Grace"` |

### SetupAllProperties

`SetupAllProperties` makes every property on the mock remember assignments. With the default mock settings, each
property starts at its default value, such as `null` for a `string` or `0` for an `int`.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
mock.SetupAllProperties();
mock.SetupGet(x => x.Id).Returns(7);

mock.Object.Name = "Grace";
string name = mock.Object.Name; // "Grace"
int id = mock.Object.Id;        // 7
```

Call `SetupAllProperties` before your other property setups. It overrides property setups made earlier, and it
resets values stored so far.

| Order | Result |
| ----- | ------ |
| `SetupGet(x => x.Id).Returns(7)`, then `SetupAllProperties()` | `Id` returns `0` |
| `SetupProperty(x => x.Name, "Ada")`, then `SetupAllProperties()` | `Name` returns `null` |
| `SetupAllProperties()`, then `SetupGet(x => x.Id).Returns(7)` | `Id` returns `7` |

Three more facts about `SetupAllProperties`, all on Moq 4.18.4 and 4.20.72:

- It covers get-only properties. `Id` returns `0` on a strict mock instead of throwing.
- It does not set up methods. `GetDisplayName()` still throws on a strict mock.
- It does not track indexers. See [Indexers](#indexers).

## Match or verify assignments: SetupSet and VerifySet

`SetupSet` sets up the property setter. Write the assignment you expect inside the lambda.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
mock.SetupSet(x => x.Name = "Grace");

mock.Object.Name = "Grace"; // allowed
```

On a strict mock, any other value throws `MockException`. Use `It.IsAny<string>()` to accept every value.

`SetupSet` does not store the value. Reading `Name` afterward throws on a strict mock and returns `null` on a loose
one. Use `SetupProperty` when the test reads the value back.

To run code on each assignment, add a callback. The type argument on `SetupSet<string>` gives the callback its
parameter type.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
var assigned = new List<string>();
mock.SetupSet<string>(x => x.Name = It.IsAny<string>())
    .Callback(value => assigned.Add(value));

mock.Object.Name = "Grace"; // assigned now holds "Grace"
```

`VerifySet` checks an assignment after the fact. It needs no setup on a loose mock.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Loose);

mock.Object.Name = "Grace";

mock.VerifySet(x => x.Name = "Grace", Times.Once());
```

If your code assigned a different value, `VerifySet` throws `MockException`. The first line of text is:

```text
Expected invocation on the mock once, but was 0 times: x => x.Name = "Grace"
```

On a strict mock, set the property up first with `SetupSet` or `SetupProperty`. Then `VerifySet` works the same way.
`VerifyGet(x => x.Name, Times.Once())` checks reads.

### Is SetupSet obsolete?

No. Only the old overloads that take a getter expression are. All of this holds on Moq 4.18.4 and 4.20.72.

- `SetupSet(x => x.Name)` is marked `[Obsolete]`. The compiler warns with CS0618 and the text
  `Replaced by SetupSet(Action)`.
- `SetupSet(x => x.Name, "Grace")` is obsolete as an error. It does not compile (CS0619).
- The matching `VerifySet(x => x.Name)` overloads are obsolete too.

The assignment forms on this page are current.

## Read-only properties and properties without a setter

`Id` has a getter and no setter. What works depends on the method. Moq1101 does not report any of these cases. Where
a call fails, the compiler or Moq reports it.

| Call on a get-only property | Result on Moq 4.18.4 and 4.20.72 |
| --------------------------- | -------------------------------- |
| `mock.SetupGet(x => x.Id).Returns(7)` | Works |
| `mock.Setup(x => x.Id).Returns(7)` | Works |
| `mock.SetupAllProperties()` | Works. `Id` returns `0` until you add a `SetupGet` after it |
| `mock.SetupProperty(x => x.Id)` | Compiles, then throws `ArgumentException` from the `SetupProperty` call |
| `mock.SetupSet(x => x.Id = 7)` | Does not compile (CS0200) |

`SetupProperty` on a get-only property throws this message. Moq fills in the type and the property name.

```text
Property ICustomer.Id does not have a setter.
```

Use `SetupGet` for a get-only property.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
mock.SetupGet(x => x.Id).Returns(7);
```

A property with a setter and no getter is the reverse. `SetupSet` and `VerifySet` work. `SetupGet`, `Setup`, and
`SetupProperty` do not compile, because their lambda reads the property (CS0154).

## Indexers

Set up an indexer getter with `Setup` or `SetupGet`. Put the index inside the lambda.

```csharp
public interface ISettings
{
    string this[string key] { get; set; }
}
```

```csharp
var mock = new Mock<ISettings>(MockBehavior.Strict);
mock.Setup(x => x["theme"]).Returns("dark");

string theme = mock.Object["theme"]; // "dark"
```

Use a matcher to answer for every index.

```csharp
var mock = new Mock<ISettings>(MockBehavior.Strict);
mock.Setup(x => x[It.IsAny<string>()]).Returns("unset");
```

`SetupSet` and `VerifySet` take the indexed assignment.

```csharp
var mock = new Mock<ISettings>(MockBehavior.Strict);
mock.SetupSet(x => x["theme"] = "light");

mock.Object["theme"] = "light";

mock.VerifySet(x => x["theme"] = "light", Times.Once());
```

`SetupProperty` does not support indexers. This call compiles and then throws `ArgumentException`:

```csharp
var mock = new Mock<ISettings>(MockBehavior.Strict);
mock.SetupProperty(x => x["theme"], "dark"); // throws at run time
```

```text
Expression is not a property access: x => x["theme"]
```

`SetupAllProperties` skips indexers as well. To make an indexer remember values, back it with a dictionary:

```csharp
var mock = new Mock<ISettings>(MockBehavior.Strict);
var store = new Dictionary<string, string>();

mock.SetupSet(x => x[It.IsAny<string>()] = It.IsAny<string>())
    .Callback((string key, string value) => store[key] = value);
mock.Setup(x => x[It.IsAny<string>()])
    .Returns((string key) => store[key]);

mock.Object["theme"] = "light";
string theme = mock.Object["theme"]; // "light"
```

## Property setups on a method (Moq1101)

`SetupGet`, `SetupSet`, and `SetupProperty` take a property. Passing a call to a method that returns a value
compiles, and [Moq1101](rules/Moq1101.md) flags it.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
mock.SetupGet(x => x.GetDisplayName()); // Moq1101
```

The build reports:

```text
warning Moq1101: SetupGet/SetupSet/SetupProperty should be used for properties, not for methods like 'GetDisplayName'
```

Use `Setup` for a method, and give it a return value.

```csharp
var mock = new Mock<ICustomer>(MockBehavior.Strict);
mock.Setup(x => x.GetDisplayName()).Returns("Ada");
```

`Setup` with no `Returns` on a method that returns a value triggers [Moq1203](rules/Moq1203.md).

Without the analyzer, the test fails when the setup line runs. On Moq 4.18.4 and 4.20.72, `SetupGet` and
`SetupProperty` throw `ArgumentException`:

```text
Expression is not a property access: x => x.GetDisplayName()
```

`SetupSet` on a method throws `ArgumentException` with different text. The three dots are part of Moq's message.

```text
Unsupported expression: x => x.GetDisplayName()...
The return type of the last member shown above is not mockable.
```

Moq1101 has no code fix. It does not check `SetupAllProperties`, `VerifyGet`, or `VerifySet`.

## Properties must be virtual or on an interface

Moq can only set up a property it can override: an interface property, or a `virtual` or `abstract` class property.
[Moq1200](rules/Moq1200.md) reports `Setup` on any other property and explains the fixes. It does not report
`SetupGet`, `SetupSet`, or `SetupProperty` on a non-virtual property. Those calls throw when the test runs. This gap
is tracked in [#1439](https://github.com/rjmurillo/moq.analyzers/issues/1439).

## FAQ

### What is the difference between SetupGet and SetupProperty in Moq?

`SetupGet` returns the value you configure, and assignments do not change it. `SetupProperty` makes the property
store and return what your code assigns.

### Is Setup the same as SetupGet for a property?

For a getter, yes. Both return the configured value, and Moq's own error text calls them equivalent. See
[Return a value from a property](#return-a-value-from-a-property-setupget-or-setup).

### How do I mock a read-only property with Moq?

Use `SetupGet(x => x.Id).Returns(value)` on an interface property or a `virtual` class property. `SetupProperty`
throws for a property with no setter.

### How do I set up all properties at once?

Call `mock.SetupAllProperties()`. Call it before your other property setups, because it overrides the ones made
earlier. See [SetupAllProperties](#setupallproperties).

### Why do I get Moq1101?

You called `SetupGet`, `SetupSet`, or `SetupProperty` on a method. Use `Setup` instead.

### Is SetupSet obsolete?

No. Only the old overloads that take a getter expression, such as `SetupSet(x => x.Name)`, are obsolete. Write the
assignment in the lambda: `SetupSet(x => x.Name = "Grace")`. See [Is SetupSet obsolete?](#is-setupset-obsolete).

### Why does my mocked property return null after I assign it?

The mock does not track the property. Call `SetupProperty` for that property, or `SetupAllProperties` for all of
them.

## See also

- [Moq1101: SetupGet/SetupSet/SetupProperty should be used for properties, not for methods](rules/Moq1101.md)
- [Moq1200: Setup should be used only for overridable members](rules/Moq1200.md)
- [Moq1400: Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior](rules/Moq1400.md)
- [Property and method setup confusion](common-moq-mistakes.md#property-and-method-setup-confusion) in the guide to
  all common Moq mistakes
- [Moq quickstart: properties](https://github.com/devlooped/moq/wiki/Quickstart#properties)
