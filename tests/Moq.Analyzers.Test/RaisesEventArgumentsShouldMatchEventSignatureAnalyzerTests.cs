using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.RaisesEventArgumentsShouldMatchEventSignatureAnalyzer>;

namespace Moq.Analyzers.Test;

public class RaisesEventArgumentsShouldMatchEventSignatureAnalyzerTests
{
    public static IEnumerable<object[]> ValidTestData()
    {
        return new object[][]
        {
            // Valid: Action<string> event with string argument
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.StringEvent += null, "test");"""],

            // Valid: Action<int> event with int argument
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.NumberEvent += null, 42);"""],

            // Valid: EventHandler<CustomArgs> event with CustomArgs argument
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.CustomEvent += null, new CustomArgs());"""],

            // Valid: Action event with no parameters
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.SimpleEvent += null);"""],

            // Valid: Custom delegate with correct arguments
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.CustomDelegate += null, "test");"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ValidRaisesOnReturnsChainTestData()
    {
        return new object[][]
        {
            // Valid: Raises on Returns chain with Action<string> event with string argument
            ["""mockProvider.Setup(x => x.GetValue()).Returns(1).Raises(x => x.StringEvent += null, "test");"""],

            // Valid: Raises on Returns chain with Action<int> event with int argument
            ["""mockProvider.Setup(x => x.GetValue()).Returns(1).Raises(x => x.NumberEvent += null, 42);"""],

            // Valid: Raises on Returns chain with EventHandler<CustomArgs> event with correct args
            ["""mockProvider.Setup(x => x.GetValue()).Returns(1).Raises(x => x.CustomEvent += null, new CustomArgs());"""],

            // Valid: Raises on Returns chain with Action event with no parameters
            ["""mockProvider.Setup(x => x.GetValue()).Returns(1).Raises(x => x.SimpleEvent += null);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidTestData()
    {
        return new object[][]
        {
            // Invalid: Action<string> event with int argument
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.StringEvent += null, {|Moq1204:42|});"""],

            // Invalid: Action<int> event with string argument
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.NumberEvent += null, {|Moq1204:"test"|});"""],

            // Invalid: EventHandler<CustomArgs> event with wrong type
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.CustomEvent += null, {|Moq1204:"wrong"|});"""],

            // Invalid: Too many arguments
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.StringEvent += null, "test", {|Moq1204:"extra"|});"""],

            // Invalid: Too few arguments
            ["""{|Moq1204:mockProvider.Setup(x => x.Submit()).Raises(x => x.StringEvent += null)|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidRaisesOnReturnsChainTestData()
    {
        return new object[][]
        {
            // Invalid: Raises on Returns chain with Action<string> event with int argument
            ["""mockProvider.Setup(x => x.GetValue()).Returns(1).Raises(x => x.StringEvent += null, {|Moq1204:42|});"""],

            // Invalid: Raises on Returns chain with Action<int> event with string argument
            ["""mockProvider.Setup(x => x.GetValue()).Returns(1).Raises(x => x.NumberEvent += null, {|Moq1204:"test"|});"""],

            // Invalid: Raises on Returns chain with EventHandler<CustomArgs> event with wrong type
            ["""mockProvider.Setup(x => x.GetValue()).Returns(1).Raises(x => x.CustomEvent += null, {|Moq1204:"wrong"|});"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(ValidTestData))]
    public async Task ShouldNotReportDiagnosticForValidRaisesArguments(string referenceAssemblyGroup, string @namespace, string raisesCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}
            using Moq;
            using System;

            internal class CustomArgs : EventArgs 
            { 
                public string Value { get; set; }
            }

            internal delegate void MyDelegate(string value);

            internal interface ITestInterface
            {
                void Submit();
                int GetValue();
                event Action<string> StringEvent;
                event Action<int> NumberEvent;
                event EventHandler<CustomArgs> CustomEvent;
                event Action SimpleEvent;
                event MyDelegate CustomDelegate;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<ITestInterface>();
                    {{raisesCall}}
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(InvalidTestData))]
    public async Task ShouldReportDiagnosticForInvalidRaisesArguments(string referenceAssemblyGroup, string @namespace, string raisesCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}
            using Moq;
            using System;

            internal class CustomArgs : EventArgs 
            { 
                public string Value { get; set; }
            }

            internal delegate void MyDelegate(string value);

            internal interface ITestInterface
            {
                void Submit();
                int GetValue();
                event Action<string> StringEvent;
                event Action<int> NumberEvent;
                event EventHandler<CustomArgs> CustomEvent;
                event Action SimpleEvent;
                event MyDelegate CustomDelegate;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<ITestInterface>();
                    {{raisesCall}}
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(ValidRaisesOnReturnsChainTestData))]
    public async Task ShouldNotReportDiagnosticForValidRaisesOnReturnsChainArguments(string referenceAssemblyGroup, string @namespace, string raisesCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}
            using Moq;
            using System;

            internal class CustomArgs : EventArgs
            {
                public string Value { get; set; }
            }

            internal delegate void MyDelegate(string value);

            internal interface ITestInterface
            {
                void Submit();
                int GetValue();
                event Action<string> StringEvent;
                event Action<int> NumberEvent;
                event EventHandler<CustomArgs> CustomEvent;
                event Action SimpleEvent;
                event MyDelegate CustomDelegate;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<ITestInterface>();
                    {{raisesCall}}
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(InvalidRaisesOnReturnsChainTestData))]
    public async Task ShouldReportDiagnosticForInvalidRaisesOnReturnsChainArguments(string referenceAssemblyGroup, string @namespace, string raisesCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}
            using Moq;
            using System;

            internal class CustomArgs : EventArgs
            {
                public string Value { get; set; }
            }

            internal delegate void MyDelegate(string value);

            internal interface ITestInterface
            {
                void Submit();
                int GetValue();
                event Action<string> StringEvent;
                event Action<int> NumberEvent;
                event EventHandler<CustomArgs> CustomEvent;
                event Action SimpleEvent;
                event MyDelegate CustomDelegate;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<ITestInterface>();
                    {{raisesCall}}
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

    [Fact]
    public async Task ShouldNotTriggerOnUserDefinedRaisesMethod()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;

            internal class MyEventEmitter
            {
                public void Raises(string eventName, EventArgs args) { }
                public void RaisesAsync(string eventName, EventArgs args) { }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var emitter = new MyEventEmitter();
                    emitter.Raises("Click", EventArgs.Empty);
                    emitter.RaisesAsync("Click", EventArgs.Empty);
                }
            }
            """,
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldNotTriggerOnUserDefinedRaisesExtensionMethod()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;

            internal static class MyExtensions
            {
                public static void Raises(this object obj, string name) { }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var target = new object();
                    target.Raises("test");
                }
            }
            """,
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldNotTriggerOnInterfaceWithRaisesMethod()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;

            internal interface IEventRaiser
            {
                void Raises(object sender, EventArgs e);
            }

            internal class MyRaiser : IEventRaiser
            {
                public void Raises(object sender, EventArgs e) { }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    IEventRaiser raiser = new MyRaiser();
                    raiser.Raises(this, EventArgs.Empty);
                }
            }
            """,
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
