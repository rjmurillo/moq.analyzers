# Mocking a class with constructor parameters in Moq

Pass the constructor arguments to `new Mock<T>(...)`, after an optional `MockBehavior`. Moq forwards them to a
matching public or protected constructor of `T`. Interfaces take no constructor arguments
([Moq1001](rules/Moq1001.md)). Arguments that match no constructor fail at run time with "Can not instantiate proxy of
class", and [Moq1002](rules/Moq1002.md) reports them at build time.

```csharp
var clock = Mock.Of<IClock>();
var mock = new Mock<OrderService>(clock, 3);

OrderService service = mock.Object;
```

The samples on this page use these types. Each sample was built and run with Moq 4.20.72 and .NET SDK 10.0.401.
Samples marked with a rule ID produce that diagnostic with
[Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers) 0.4.2.

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
}

public class OrderService
{
    public OrderService(IClock clock, int retries) { }

    public virtual bool Place(string id) => true;
}
```

## Contents

- [Pass constructor arguments to Mock\<T\>](#pass-constructor-arguments-to-mockt)
- [Use a lambda instead (compile-checked)](#use-a-lambda-instead-compile-checked)
- [Can not instantiate proxy of class: Could not find a parameterless constructor](#can-not-instantiate-proxy-of-class-could-not-find-a-parameterless-constructor)
- [Interfaces take no constructor arguments (Moq1001)](#interfaces-take-no-constructor-arguments-moq1001)
- [Arguments that match no constructor (Moq1002)](#arguments-that-match-no-constructor-moq1002)
- [Optional and params parameters](#optional-and-params-parameters)
- [Private, protected, and internal constructors](#private-protected-and-internal-constructors)
- [Mocking framework classes (DbContext, UserManager)](#mocking-framework-classes-dbcontext-usermanager)
- [Constructor arguments do not make members mockable](#constructor-arguments-do-not-make-members-mockable)
- [FAQ](#faq)
- [See also](#see-also)

## Pass constructor arguments to Mock\<T\>

List the arguments in constructor order. To set a `MockBehavior`, put it first. Moq keeps the behavior for itself and
passes the rest to the constructor.

```csharp
var clock = Mock.Of<IClock>();

var mock = new Mock<OrderService>(clock, 3);
var strict = new Mock<OrderService>(MockBehavior.Strict, clock, 3);
Mock<OrderService> targetTyped = new(clock, 3);
```

`MockRepository.Create<T>` takes the same arguments.

```csharp
var clock = Mock.Of<IClock>();
var repository = new MockRepository(MockBehavior.Strict);

Mock<OrderService> mock = repository.Create<OrderService>(clock, 3);
```

Moq does not call the constructor when you write `new Mock<T>(...)`. It calls the constructor the first time you
read `mock.Object`. A wrong argument list on a class mock throws there.

`Mock.Of<T>` has no overload that takes constructor arguments. Use it for interfaces and for classes that have a
parameterless constructor.

## Use a lambda instead (compile-checked)

Pass a lambda that calls the constructor. The compiler checks the arguments, so a wrong argument list fails the
build. To set a `MockBehavior`, put it after the lambda.

```csharp
var clock = Mock.Of<IClock>();

var mock = new Mock<OrderService>(() => new OrderService(clock, 3));
var strict = new Mock<OrderService>(() => new OrderService(clock, 3), MockBehavior.Strict);
```

This form needs Moq 4.13.0 or later. It fails on Moq 4.12.0 and works on Moq 4.13.0.

On Moq 4.12.0, the same line still compiles with C# 10 or later. Moq then passes the lambda itself as a constructor
argument, and `mock.Object` throws. With C# 9, the compiler rejects the line with CS1660.

```text
Can not instantiate proxy of class: OrderService.
Could not find a constructor that would match given arguments:
System.Func`1[OrderService]
```

The lambda body must be a constructor call. A factory method throws `System.NotSupportedException` when the mock is
created.

```csharp
public static class OrderFactory
{
    public static OrderService Create(IClock clock) => new OrderService(clock, 3);
}

var clock = Mock.Of<IClock>();

var mock = new Mock<OrderService>(() => OrderFactory.Create(clock)); // throws
```

Moq 4.20.72 reports:

```text
Unsupported expression: OrderFactory.Create(clock)
```

Moq1002 does not check the lambda form. On Moq 4.13.0 or later, the compiler already does. On Moq 4.12.0, nothing
checks it before the test runs.

## Can not instantiate proxy of class: Could not find a parameterless constructor

You get this error when the class has no parameterless constructor that Moq can call, and you passed no arguments.

```csharp
var mock = new Mock<OrderService>(); // Moq1002

OrderService service = mock.Object;  // throws
```

The build reports:

```text
warning Moq1002: Could not find a matching constructor for type 'OrderService' with arguments ()
```

Without the analyzer, the code compiles. Moq 4.18.4 and 4.20.72 throw `System.ArgumentException` when you read
`mock.Object`:

```text
Can not instantiate proxy of class: OrderService.
Could not find a parameterless constructor. (Parameter 'constructorArguments')
```

The inner exception is `System.MissingMethodException`:

```text
Constructor on type 'Castle.Proxies.OrderServiceProxy' not found.
```

The text comes from Castle.Core, the library Moq uses to build the mock. It shows the full type name, with the
namespace.

Moq 4.8.2 and 4.17.1 use Castle.Core 4. They print the same two lines without the `(Parameter ...)` part. They throw
`Castle.DynamicProxy.InvalidProxyConstructorArgumentsException`, which derives from `ArgumentException`.

`Mock.Of<OrderService>()` and `new Mock<OrderService>(MockBehavior.Strict)` throw the same exception. `Mock.Of`
throws on the call itself. Moq1002 reports both.

Fix it in one of three ways.

Pass the constructor arguments:

```csharp
var clock = Mock.Of<IClock>();
var mock = new Mock<OrderService>(clock, 3);

OrderService service = mock.Object;
```

Or add a parameterless constructor to a class you own. `protected` is enough, because the mock is a subclass.

```csharp
public class AuditService
{
    public AuditService(IClock clock) { }

    protected AuditService() { }

    public virtual void Record(string entry) { }
}

var mock = new Mock<AuditService>();

AuditService service = mock.Object;
```

Or mock an interface. An interface mock needs no constructor.

```csharp
public interface IOrderService
{
    bool Place(string id);
}

var mock = new Mock<IOrderService>();

IOrderService service = mock.Object;
```

## Interfaces take no constructor arguments (Moq1001)

An interface has no constructor, so Moq has nowhere to send the arguments.

```csharp
var mock = new Mock<IClock>(42); // Moq1001
```

```text
warning Moq1001: Mocked interface 'IClock' cannot have constructor parameters (42)
```

Without the analyzer, the code compiles. Moq throws `System.ArgumentException` on that line, before you read
`mock.Object`. Checked at Moq 4.8.2, 4.18.4, and 4.20.72:

```text
Constructor arguments cannot be passed for interface mocks.
```

Remove the arguments. A `MockBehavior` is not a constructor argument, so you can keep it.

```csharp
var mock = new Mock<IClock>();
var strict = new Mock<IClock>(MockBehavior.Strict);
```

See [Moq1001](rules/Moq1001.md).

## Arguments that match no constructor (Moq1002)

Moq1002 checks the number of arguments and whether each argument converts to the parameter type.

```csharp
var mock = new Mock<OrderService>("three"); // Moq1002

OrderService service = mock.Object;         // throws
```

```text
warning Moq1002: Could not find a matching constructor for type 'OrderService' with arguments ("three")
```

Without the analyzer, the code compiles. Moq 4.18.4 and 4.20.72 throw `System.ArgumentException` when you read
`mock.Object`. The message lists the type of each argument you passed, one per line. A `null` argument prints as
`<null>`.

```text
Can not instantiate proxy of class: OrderService.
Could not find a constructor that would match given arguments:
System.String
 (Parameter 'constructorArguments')
```

Pass arguments that match a constructor:

```csharp
var clock = Mock.Of<IClock>();
var mock = new Mock<OrderService>(clock, 3);

OrderService service = mock.Object;
```

Moq1002 also reports constructor arguments on a delegate mock. Moq throws `System.ArgumentException` on that line
with the message `Constructor arguments cannot be passed for delegate mocks.`

```csharp
var mock = new Mock<Func<int, int>>(1); // Moq1002
```

See [Moq1002](rules/Moq1002.md).

## Optional and params parameters

Pass every argument, including optional ones. Moq does not fill in default values. Moq1002 does not report a missing
optional argument, so this mistake shows up only when the test runs.

```csharp
public class ReportService
{
    public ReportService(string title, int copies = 1) { }
}

var mock = new Mock<ReportService>("Q3"); // no diagnostic

ReportService service = mock.Object;      // throws
```

```text
Can not instantiate proxy of class: ReportService.
Could not find a constructor that would match given arguments:
System.String
 (Parameter 'constructorArguments')
```

```csharp
public class ReportService
{
    public ReportService(string title, int copies = 1) { }
}

var mock = new Mock<ReportService>("Q3", 1);

ReportService service = mock.Object;
```

A `params` parameter works. Pass the values one by one, or pass none.

```csharp
public class BatchService
{
    public BatchService(string name, params int[] sizes) { }
}

var some = new Mock<BatchService>("nightly", 10, 20);
var none = new Mock<BatchService>("nightly");

BatchService first = some.Object;
BatchService second = none.Object;
```

Moq also accepts the values as one array. Moq1002 reports that form even though it runs.

```csharp
public class BatchService
{
    public BatchService(string name, params int[] sizes) { }
}

var mock = new Mock<BatchService>("nightly", new[] { 10, 20 }); // Moq1002, but it runs

BatchService service = mock.Object;
```

## Private, protected, and internal constructors

The mock is a subclass that Castle.Core generates in another assembly, named `DynamicProxyGenAssembly2`. Moq can
call only the constructors that subclass can reach. Checked at Moq 4.20.72:

| Constructor | Result at run time                                                                  | Moq1002 |
| ----------- | ----------------------------------------------------------------------------------- | ------- |
| `public`    | Works                                                                               | Silent  |
| `protected` | Works                                                                               | Silent  |
| `internal`  | Works only when the assembly grants `InternalsVisibleTo("DynamicProxyGenAssembly2")` | Silent  |
| `private`   | Fails                                                                               | Reports |

When the class has no constructor the mock can reach, Moq throws `System.NotSupportedException` when you read
`mock.Object`:

```text
Parent does not have a default constructor. The default constructor must be explicitly defined.
```

When the class also has a constructor the mock can reach, you get the "Can not instantiate proxy of class" error
instead.

To open `internal` members to the mock, add this attribute to the assembly that declares the class.
[Moq1003](rules/Moq1003.md) covers the same attribute for internal types.

```csharp
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]
```

## Mocking framework classes (DbContext, UserManager)

Framework classes fail the same way: the class has no parameterless constructor, and the test passes no arguments.
Read the constructor signature of the class and pass one argument for each parameter. The samples below were built and
run with Microsoft.EntityFrameworkCore 10.0.0 and Microsoft.Extensions.Identity.Core 10.0.0. Check the signature in
the version you use.

This `DbContext` subclass has one constructor, and it takes options. Pass an options object.

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}

var options = new DbContextOptions<AppDbContext>();
var mock = new Mock<AppDbContext>(options);

AppDbContext context = mock.Object;
```

`UserManager<TUser>` has one public constructor with nine parameters. Pass a mock for the store and `null` for the
rest.

```csharp
public class AppUser
{
}

var store = Mock.Of<IUserStore<AppUser>>();
var mock = new Mock<UserManager<AppUser>>(store, null, null, null, null, null, null, null, null);

UserManager<AppUser> manager = mock.Object;
```

## Constructor arguments do not make members mockable

Constructor arguments only build the object. They do not change which members Moq can intercept.

- Moq can set up only `virtual`, `abstract`, or interface members. See [Moq1200](rules/Moq1200.md).
- Moq cannot mock a `sealed` class, with or without constructor arguments. See [Moq1000](rules/Moq1000.md).
- A real constructor runs real code. If the constructor does work you do not want in a test, mock an interface
  instead.

## FAQ

### How do I mock a class with constructor parameters in Moq?

Pass the arguments to `new Mock<T>(arg1, arg2)`, after an optional `MockBehavior`. Moq passes them to a matching
constructor.

### What does "Can not instantiate proxy of class. Could not find a parameterless constructor" mean?

The class has no parameterless constructor Moq can call, and you passed no arguments. Pass the constructor arguments,
or mock an interface.

### Can I pass constructor arguments when mocking an interface?

No. Interfaces have no constructors, so Moq throws `Constructor arguments cannot be passed for interface mocks.`
[Moq1001](rules/Moq1001.md) flags the call at build time.

### Can Moq call a private or internal constructor?

Moq cannot call a `private` constructor. It can call an `internal` constructor only when the assembly grants
`InternalsVisibleTo("DynamicProxyGenAssembly2")`. It can call `public` and `protected` constructors.

### Is there a compile-safe way to pass constructor arguments?

Yes. Use the lambda form, `new Mock<T>(() => new T(arg1, arg2))`. It needs Moq 4.13.0 or later.

### Does Moq1002 check parameter types or only the count?

Both. It checks the argument count and whether each argument converts to the parameter type. It does not report a
missing optional argument.

### Why does the error appear at mock.Object and not at new Mock\<T\>(...)?

Moq builds the mocked object the first time you read `mock.Object`. The constructor runs then. Interface and delegate
mocks are the exception: Moq rejects their constructor arguments as soon as you create the mock.

## See also

- [Moq1001](rules/Moq1001.md): Mocked interfaces cannot have constructor parameters
- [Moq1002](rules/Moq1002.md): Parameters provided into mock do not match any existing constructors
- [Moq1003](rules/Moq1003.md): Internal type requires InternalsVisibleTo for DynamicProxy
- [Moq1000](rules/Moq1000.md): Sealed classes cannot be mocked
- [Moq1200](rules/Moq1200.md): Setup should be used only for overridable members
- [Constructor argument mistakes](common-moq-mistakes.md#constructor-argument-mistakes) in the guide to all common
  Moq mistakes
