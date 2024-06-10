# Moq.Analyzers

**Moq.Analyzers** is a Roslyn analyzer that helps to write unit tests using the popular and friend
[Moq](https://github.com/devlooped/moq) library. Moq.Analyzers protects you from popular mistakes and warns you if
something is wrong with your Moq configuration:

## Detected issues

* Moq1000 = Sealed classes cannot be mocked.
* Moq1001 = Mocked interfaces cannot have constructor parameters.
* Moq1002 = Parameters provided into mock do not match any existing constructors.
* Moq1100 = Callback signature must match the signature of the mocked method.
* Moq1101 = SetupGet/SetupSet should be used for properties, not for methods.
* Moq1200 = Setup should be used only for overridable members.
* Moq1201 = Setup of async methods should use `.ReturnsAsync` instance instead of `.Result`.
* Moq1300 = Mock.As() should take interfaces.

## How to install

Install ["Moq.Analyzers" NuGet package](https://www.nuget.org/packages/Moq.Analyzers) into test projects using Moq.

You must use an in-support version of the .NET SDK (i.e. 6+).

## Contributions welcome

Moq.Analyzers continues to evolve and add new features. Any help will be appreciated. You can report issues,
develop new features, improve the documentation, or do other cool stuff. See [CONTRIBUTING.md](./CONTRIBUTING.md).
