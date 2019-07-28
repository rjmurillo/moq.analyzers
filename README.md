# Moq.Analyzers

Visual Studio extension and Roslyn analyzer that helps to write unit tests using Moq mocking library. Port of Resharper extension to Roslyn.

Note: If you are using Visual Studio 2017 then you can additionally install [Moq.Autocomplete](https://github.com/Litee/moq.autocomplete) extension/package to get better autocomplete suggestions when writing code.

## Detected issues

* Moq1000 = Sealed classes cannot be mocked
* Moq1001 = Mocked interfaces cannot have constructor parameters
* Moq1002 = Parameters provided into mock do not match any existing constructors
* Moq1100 = Callback signature must match the signature of the mocked method
* Moq1101 = SetupGet/SetupSet should be used for properties, not for methods
* Moq1200 = Setup should be used only for overridable members.

## TODO

* Setup() should be used for methods, not for properties
* Mock.As<T>() should take interfaces only
* AdvancedMatcherAttribute should accept only IMatcher types
* Checks for protected mocks when dynamically referencing their members
* Setup() requires overridable members
* Mock.Get() should not take literals
 
## How to install:

* (Option 1) Install "Moq.Analyzers" NuGet package into test projects. Con: Extension will work for specific projects only. Pro: It will be available for all project developers automatically.
* (Option 2) Install "Moq.Analyzers" extension into Visual Studio. Con: Every developer must install extension manually. Pro: It works for all your projects.
