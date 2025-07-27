using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.EventSetupHandlerShouldMatchEventTypeAnalyzer>;

namespace Moq.Analyzers.Test;

public class EventSetupHandlerShouldMatchEventTypeAnalyzerTests
{
    // Only one version of each static data source method
    public static IEnumerable<object[]> InvalidTestData()
    {
        return new object[][]
        {
            // null assignment is a valid C# event handler operation, but not a Moq error
            ["""mockProvider.SetupAdd(x => x.StringEvent += null);"""],

            // Mismatched event handler type, compiler emits CS0029 for Action<int> to Action<string> (span 23,36,23,71)
            ["""mockProvider.SetupAdd(x => {|CS0029:x.StringEvent += It.IsAny<Action<int>>()|});"""],

            // Method group assignment, compiler emits CS0131 (not assignable)
            ["""mockProvider.SetupAdd(x => {|CS0131:x.ToString()|} += It.IsAny<Action>());"""],

            // Mismatched event handler type, compiler emits CS0029 for Action to EventHandler<CustomArgs> (span 23,36,23,71)
            ["""mockProvider.SetupAdd(x => {|CS0029:x.CustomEvent += It.IsAny<Action>()|});"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ValidTestData()
    {
        return new object[][]
        {
            ["""mockProvider.SetupAdd(x => x.StringEvent += It.IsAny<Action<string>>());"""],
            ["""mockProvider.SetupAdd(x => x.CustomEvent += It.IsAny<EventHandler<CustomArgs>>());"""],
            ["""mockProvider.SetupAdd(x => x.SimpleEvent += It.IsAny<Action>());"""],
            ["""mockProvider.SetupRemove(x => x.StringEvent -= It.IsAny<Action<string>>());"""],
            ["""mockProvider.SetupAdd(x => x.CustomDelegate += It.IsAny<MyDelegate>());"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(InvalidTestData))]
    public async Task ShouldHandleInvalidEventSetups(string referenceAssemblyGroup, string @namespace, string setupCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}

            internal class CustomArgs : EventArgs
            {
                public string Value { get; set; }
            }

            internal delegate void MyDelegate(string value);

            internal interface ITestInterface
            {
                event Action<string> StringEvent;
                event EventHandler<CustomArgs> CustomEvent;
                event Action SimpleEvent;
                event MyDelegate CustomDelegate;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<ITestInterface>();
                    {{setupCall}}
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(ValidTestData))]
    public async Task ShouldNotReportDiagnosticForValidEventSetup(string referenceAssemblyGroup, string @namespace, string setupCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}

            internal class CustomArgs : EventArgs
            {
                public string Value { get; set; }
            }

            internal delegate void MyDelegate(string value);

            internal interface ITestInterface
            {
                event Action<string> StringEvent;
                event EventHandler<CustomArgs> CustomEvent;
                event Action SimpleEvent;
                event MyDelegate CustomDelegate;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<ITestInterface>();
                    {{setupCall}}
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
