using Microsoft.CodeAnalysis.Testing;
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

    public static IEnumerable<object[]> EventHandlerShapedTestData()
    {
        return new object[][]
        {
            // Canonical Moq pattern for non-generic EventHandler: Moq supplies the sender.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.Closed += null, EventArgs.Empty);"""],

            // A derived EventArgs is implicitly convertible to the base EventHandler parameter, so the
            // statically-known exact runtime type upcasts cleanly.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.Closed += null, new CustomEventArgs());"""],

            // Two-argument form binds Raises(..., params object[]) and is also legal.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.Closed += null, new object(), EventArgs.Empty);"""],

            // Two-argument form with a null-typed sender argument is legal (null binds to any reference).
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.Closed += null, null, EventArgs.Empty);"""],

            // EventHandler<CustomEventArgs>: args-only form (existing behavior).
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, new CustomEventArgs());"""],

            // EventHandler<CustomEventArgs>: two-argument form (previously a false positive).
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, new object(), new CustomEventArgs());"""],

            // Custom (object sender, TArgs e) delegate: both forms are legal.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.ShapedEvent += null, new CustomEventArgs());"""],
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.ShapedEvent += null, new object(), new CustomEventArgs());"""],

            // Invalid: payload not convertible to EventArgs.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.Closed += null, {|Moq1204:42|});"""],

            // Invalid: object payload is only EXPLICITLY convertible to EventArgs. Overload resolution
            // binds the params-object array overload instead of the EventArgs overload, so Moq does not
            // supply the sender and the one-argument form throws at runtime and must be flagged.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.Closed += null, {|Moq1204:new object()|});"""],

            // Invalid: an unrelated EventArgs subclass has no conversion to the CustomEventArgs delegate
            // parameter, so Moq cannot bind it even though it selects the sender-supplying overload.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, {|Moq1204:new OtherEventArgs()|});"""],

            // Invalid: EventArgs.Empty is a base EventArgs singleton. Moq selects the sender-supplying
            // overload, then casts the payload to CustomEventArgs at runtime and throws because the exact
            // runtime type is System.EventArgs. The exact type is statically known, so it is flagged.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, {|Moq1204:EventArgs.Empty|});"""],

            // Invalid: a freshly constructed base EventArgs has a statically-known exact runtime type that
            // cannot be cast to the derived CustomEventArgs delegate parameter.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, {|Moq1204:new EventArgs()|});"""],

            // Invalid: a user-defined explicit conversion exists to CustomEventArgs, but Moq's reflection
            // cast never invokes user-defined operators, so the freshly constructed SourceEventArgs throws.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, {|Moq1204:new SourceEventArgs()|});"""],

            // Valid (tolerated): an EventArgs-typed local can hold a derived CustomEventArgs at runtime, so
            // the downcast is statically indistinguishable from a valid one. Moq accepts it, so no flag.
            ["""EventArgs downcastArgs = new CustomEventArgs(); mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, downcastArgs);"""],

            // Valid (tolerated): a field reference other than EventArgs.Empty has an unknown exact runtime
            // type, so the downcast is tolerated exactly like the local-variable case above.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, SharedArgs);"""],

            // Valid (tolerated): an explicit cast wraps an object-creation whose exact runtime type derives
            // from the delegate parameter, so the cast is unwrapped and the payload accepted.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, (EventArgs)new CustomEventArgs());"""],

            // Invalid: an explicit cast wraps an object-creation whose exact runtime type is an unrelated
            // sibling. The cast is unwrapped and the payload flagged because it can never bind.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, {|Moq1204:(EventArgs)new OtherEventArgs()|});"""],

            // Invalid: nested casts wrap a base EventArgs object-creation. All conversions are unwrapped to
            // reveal the exact runtime type, which cannot be cast to the derived delegate parameter.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, {|Moq1204:(EventArgs)(object)new EventArgs()|});"""],

            // Invalid: an EventArgs-typed local declared as a type with only a user-defined conversion to
            // the delegate parameter. Moq's reflection cast never invokes user-defined operators, so it is
            // flagged even though its runtime type is unknown to the analyzer.
            ["""SourceEventArgs sourceArgs = new SourceEventArgs(); mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, {|Moq1204:sourceArgs|});"""],

            // Valid: an explicit user-defined conversion is applied at the call site, so the operator runs and
            // constructs a genuine CustomEventArgs before Moq receives it. The conversion is not unwrapped
            // because it does not preserve object identity.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, (CustomEventArgs)new SourceEventArgs());"""],

            // Valid: an implicit user-defined conversion to the delegate argument type is applied at the call
            // site. The operator runs and Moq receives a genuine CustomEventArgs, so no diagnostic is reported.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, new ConvertibleToCustom());"""],

            // Invalid: an implicit user-defined conversion produces an unrelated sibling (OtherEventArgs). The
            // operator runs and Moq receives an OtherEventArgs, which cannot be cast to CustomEventArgs.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, {|Moq1204:new ConvertibleToOther()|});"""],

            // Valid: a conditional expression whose runtime type is not statically known. The value is a
            // CustomEventArgs at runtime, so no diagnostic is reported (matches the unknown-runtime-type path).
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, true ? new CustomEventArgs() : new CustomEventArgs());"""],

            // Valid: a null-coalescing expression yields a CustomEventArgs. The runtime type is tolerated as a
            // reference or identity conversion to the delegate argument type, so no diagnostic is reported.
            ["""CustomEventArgs maybeArgs = new CustomEventArgs(); mockProvider.Setup(x => x.Submit()).Raises(n => n.CustomEvent += null, maybeArgs ?? new CustomEventArgs());"""],

            // Invalid: three arguments for a two-parameter delegate.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.Closed += null, new object(), EventArgs.Empty, {|Moq1204:"extra"|});"""],

            // Invalid: no arguments at all.
            ["""{|Moq1204:mockProvider.Setup(x => x.Submit()).Raises(n => n.Closed += null)|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> MultiParameterAndNestedGenericTestData()
    {
        return new object[][]
        {
            // Action<T1, T2> is resolved through the Invoke signature.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.PairChanged += null, 1, "two");"""],
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.PairChanged += null, 1, {|Moq1204:2|});"""],

            // Nested generic payload.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.ItemsChanged += null, new List<Dictionary<string, int>>());"""],
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.ItemsChanged += null, {|Moq1204:new List<int>()|});"""],

            // EventHandler<T> where T does not derive from EventArgs: full form required.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.StringHandler += null, new object(), "payload");"""],
            ["""{|Moq1204:mockProvider.Setup(x => x.Submit()).Raises(n => n.StringHandler += null, "payload")|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> UnresolvableDelegateTestData()
    {
        return new object[][]
        {
            // The delegate type does not resolve (mid-edit code): no Moq1204 must be reported.
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.Broken += null, 42);"""],
            ["""mockProvider.Setup(x => x.Submit()).Raises(n => n.Broken += null, EventArgs.Empty, "extra");"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
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
    [MemberData(nameof(EventHandlerShapedTestData))]
    public async Task ShouldHandleEventHandlerShapedDelegates(string referenceAssemblyGroup, string @namespace, string raisesCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}
            using Moq;
            using System;

            internal class CustomEventArgs : EventArgs { }

            internal class OtherEventArgs : EventArgs { }

            internal class SourceEventArgs : EventArgs
            {
                public static explicit operator CustomEventArgs(SourceEventArgs value) => new CustomEventArgs();
            }

            internal class ConvertibleToCustom
            {
                public static implicit operator CustomEventArgs(ConvertibleToCustom value) => new CustomEventArgs();
            }

            internal class ConvertibleToOther
            {
                public static implicit operator OtherEventArgs(ConvertibleToOther value) => new OtherEventArgs();
            }

            internal delegate void ShapedEventHandler(object sender, CustomEventArgs e);

            internal interface INotifier
            {
                void Submit();
                event EventHandler Closed;
                event EventHandler<CustomEventArgs> CustomEvent;
                event ShapedEventHandler ShapedEvent;
            }

            internal class UnitTest
            {
                private static readonly EventArgs SharedArgs = new CustomEventArgs();

                private void Test()
                {
                    var mockProvider = new Mock<INotifier>();
                    {{raisesCall}}
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MultiParameterAndNestedGenericTestData))]
    public async Task ShouldHandleMultiParameterAndNestedGenericDelegates(string referenceAssemblyGroup, string @namespace, string raisesCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}
            using Moq;
            using System;
            using System.Collections.Generic;

            internal interface INotifier
            {
                void Submit();
                event Action<int, string> PairChanged;
                event Action<List<Dictionary<string, int>>> ItemsChanged;
                event EventHandler<string> StringHandler;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<INotifier>();
                    {{raisesCall}}
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(UnresolvableDelegateTestData))]
    public async Task ShouldNotReportWhenEventDelegateTypeDoesNotResolve(string referenceAssemblyGroup, string @namespace, string raisesCall)
    {
        // CompilerDiagnostics.None suppresses CS0246 for the intentionally-unresolved delegate type,
        // mirroring the malformed-code pattern used in MockRepositoryVerifyAnalyzerTests.cs:327.
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}
            using Moq;
            using System;

            internal interface IBrokenNotifier
            {
                void Submit();
                event UnresolvedDelegateType Broken;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<IBrokenNotifier>();
                    {{raisesCall}}
                }
            }
            """,
            referenceAssemblyGroup,
            CompilerDiagnostics.None);
    }

    [Theory]
    [MemberData(nameof(UnresolvableDelegateTestData))]
    public async Task ShouldNotReportWhenEventTypeIsNotDelegate(string referenceAssemblyGroup, string @namespace, string raisesCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}
            using Moq;
            using System;

            internal interface IBrokenNotifier
            {
                void Submit();
                event string Broken;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<IBrokenNotifier>();
                    {{raisesCall}}
                }
            }
            """,
            referenceAssemblyGroup,
            CompilerDiagnostics.None);
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
