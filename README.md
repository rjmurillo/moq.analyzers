# Moq.Analyzers

Visual Studio extension and Roslyn analyzer that helps to write unit tests using Moq mocking library. Port of Resharper extension to Roslyn.

Note: If you are using Visual Studio 2017 then you can additionally install [Moq.Autocomplete](https://github.com/Litee/moq.autocomplete) extension/package to get better autocomplete suggestions when writing code.

## Detected issues

* MOQ1001 = Sealed classes cannot be mocked
* MOQ1002 = Interface mocks cannot take additional arguments
* MOQ1003 = Additional arguments for mocked class must match one of class constructors
* MOQ1101 = Highlight Callback() and Returns() methods with signatures not matching mocked methods
 
## How to install:

* (Option 1) Install "Moq.Analyzers" NuGet package into test projects. Con: Extension will work for specific projects only. Pro: It will be available for all project developers automatically.
* (Option 2) Install "Moq.Analyzers" extension into Visual Studio. Con: Every developer must install extension manually. Pro: It works for all your projects.
