using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.RaiseEventArgumentsShouldMatchEventSignatureAnalyzer>;

namespace Moq.Analyzers.Test;

public class RaiseEventArgumentsShouldMatchEventSignatureAnalyzerTests
{
    public static IEnumerable<object[]> ValidTestData()
    {
        return new object[][]
        {
            // Valid: Action<string> event with string argument
            ["""mockProvider.Raise(p => p.StringOptionsChanged += null, "correct");"""],

            // Valid: Action<int> event with int argument
            ["""mockProvider.Raise(p => p.NumberChanged += null, 42);"""],

            // Valid: Action<MyOptions> event with MyOptions argument
            ["""mockProvider.Raise(p => p.OptionsChanged += null, new MyOptions());"""],

            // Valid: Action with no parameters
            ["""mockProvider.Raise(p => p.SimpleEvent += null);"""],

            // Valid: Implicit conversion from int to double
            ["""mockProvider.Raise(p => p.DoubleChanged += null, 42);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidTestData()
    {
        return new object[][]
        {
            // Invalid: Action<string> event with int argument
            ["""mockProvider.Raise(p => p.StringOptionsChanged += null, {|Moq1500:42|});"""],

            // Invalid: Action<MyOptions> event with wrong type
            ["""mockProvider.Raise(p => p.OptionsChanged += null, {|Moq1500:new Incorrect()|});"""],

            // Invalid: Too many arguments
            ["""mockProvider.Raise(p => p.SimpleEvent += null, {|Moq1500:"extra"|});"""],

            // Invalid: Too few arguments
            ["""{|Moq1500:mockProvider.Raise(p => p.StringOptionsChanged += null)|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(ValidTestData))]
    public async Task ShouldNotReportDiagnosticForValidRaiseArguments(string referenceAssemblyGroup, string @namespace, string raiseCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              internal class MyOptions { }
              internal class Incorrect { }

              internal interface IOptionsProvider
              {
                  event Action<string> StringOptionsChanged;
                  event Action<int> NumberChanged; 
                  event Action<MyOptions> OptionsChanged;
                  event Action SimpleEvent;
                  event Action<double> DoubleChanged;
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mockProvider = new Mock<IOptionsProvider>();
                      {{raiseCall}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(InvalidTestData))]
    public async Task ShouldReportDiagnosticForInvalidRaiseArguments(string referenceAssemblyGroup, string @namespace, string raiseCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              internal class MyOptions { }
              internal class Incorrect { }

              internal interface IOptionsProvider
              {
                  event Action<string> StringOptionsChanged;
                  event Action<MyOptions> OptionsChanged;
                  event Action SimpleEvent;
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mockProvider = new Mock<IOptionsProvider>();
                      {{raiseCall}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldHandleEventHandlerPattern()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;
            using System;

            internal class CustomEventArgs : EventArgs 
            { 
                public string Value { get; set; }
            }

            internal interface INotifier
            {
                event EventHandler<CustomEventArgs> CustomEvent;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<INotifier>();
                    // Valid: EventHandler<CustomEventArgs> expects CustomEventArgs argument only when used with Moq.Raise
                    mock.Raise(n => n.CustomEvent += null, new CustomEventArgs { Value = "test" });
                    
                    // Invalid: Wrong argument type
                    mock.Raise(n => n.CustomEvent += null, {|Moq1500:"wrong"|});
                }
            }
            """);
    }
}
