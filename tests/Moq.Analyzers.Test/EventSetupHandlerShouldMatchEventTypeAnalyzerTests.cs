using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.EventSetupHandlerShouldMatchEventTypeAnalyzer>;

namespace Moq.Analyzers.Test;

public class EventSetupHandlerShouldMatchEventTypeAnalyzerTests
{
    public static IEnumerable<object[]> ValidTestData()
    {
        return new object[][]
        {
            // Valid: Action<string> event with It.IsAny<Action<string>>
            ["""mockProvider.SetupAdd(x => x.StringEvent += It.IsAny<Action<string>>());"""],

            // Valid: EventHandler<CustomArgs> event with It.IsAny<EventHandler<CustomArgs>>
            ["""mockProvider.SetupAdd(x => x.CustomEvent += It.IsAny<EventHandler<CustomArgs>>());"""],

            // Valid: Action event with It.IsAny<Action>
            ["""mockProvider.SetupAdd(x => x.SimpleEvent += It.IsAny<Action>());"""],

            // Valid: SetupRemove with correct handler type
            ["""mockProvider.SetupRemove(x => x.StringEvent -= It.IsAny<Action<string>>());"""],

            // Valid: Custom delegate with correct handler type
            ["""mockProvider.SetupAdd(x => x.CustomDelegate += It.IsAny<MyDelegate>());"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();
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
