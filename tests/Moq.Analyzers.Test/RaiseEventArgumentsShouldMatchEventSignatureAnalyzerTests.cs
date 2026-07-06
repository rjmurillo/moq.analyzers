using Microsoft.CodeAnalysis.Testing;
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

            // Valid: Action<string, int> event with matching arguments
            ["""mockProvider.Raise(p => p.PairChanged += null, "key", 42);"""],

            // Valid: nested generic payload Action<List<string>>
            ["""mockProvider.Raise(p => p.ListChanged += null, new List<string>());"""],

            // Valid: null literal for a reference-type parameter. The cast is required:
            // a bare 'null' is ambiguous between Raise(..., EventArgs) and Raise(..., params object[]) (CS0121).
            ["""mockProvider.Raise(p => p.StringOptionsChanged += null, (string)null);"""],

            // Same method name on a non-Moq type must still be rejected by the symbol check
            ["""new CustomRaiser().Raise(p => p.StringOptionsChanged += null, 42);"""],

            // Direct generic invocation covers the shape-tolerant pre-filter's GenericNameSyntax path
            ["""Raise<int>();"""],

            // Delegate-array invocation has an unknown shape and must fall through to the symbol check
            ["""Action[] actions = new Action[] { () => { } }; actions[0]();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidTestData()
    {
        return new object[][]
        {
            // Invalid: Action<string> event with int argument (wrap only the problematic argument)
            ["""mockProvider.Raise(p => p.StringOptionsChanged += null, {|Moq1202:42|});"""],

            // Invalid: Action<MyOptions> event with wrong type (wrap only the problematic argument)
            ["""mockProvider.Raise(p => p.OptionsChanged += null, {|Moq1202:new Incorrect()|});"""],

            // Invalid: Too many arguments (wrap only the extra argument)
            ["""mockProvider.Raise(p => p.SimpleEvent += null, {|Moq1202:"extra"|});"""],

            // Invalid: Too few arguments (wrap the entire invocation)
            ["""{|Moq1202:mockProvider.Raise(p => p.StringOptionsChanged += null)|};"""],

            // Invalid: wrong second argument for Action<string, int> (wrap only the problematic argument)
            ["""mockProvider.Raise(p => p.PairChanged += null, "key", {|Moq1202:"wrong"|});"""],

            // Invalid: too few arguments for Action<string, int> (wrap the entire invocation)
            ["""{|Moq1202:mockProvider.Raise(p => p.PairChanged += null, "key")|};"""],

            // Invalid: wrong generic payload - List<int> supplied to Action<List<string>>
            ["""mockProvider.Raise(p => p.ListChanged += null, {|Moq1202:new List<int>()|});"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> MockAccessPatternTestData()
    {
        return new object[][]
        {
            // Valid: Raise on a mock exposed as a property
            ["""wrapper.ProviderMock.Raise(p => p.StringOptionsChanged += null, "ok");"""],

            // Invalid: Raise on a mock exposed as a property
            ["""wrapper.ProviderMock.Raise(p => p.StringOptionsChanged += null, {|Moq1202:42|});"""],

            // Invalid: Raise on a mock returned from a method call
            ["""wrapper.GetMock().Raise(p => p.StringOptionsChanged += null, {|Moq1202:42|});"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidTestData2()
    {
        return new object[][]
        {
            // Valid: EventHandler<CustomEventArgs> expects CustomEventArgs argument only when used with Moq.Raise
            ["""mock.Raise(n => n.CustomEvent += null, new CustomEventArgs { Value = "test" });"""],

            // Invalid: Wrong argument type
            ["""mock.Raise(n => n.CustomEvent += null, {|Moq1202:"wrong"|});"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> EventHandlerShapedTestData()
    {
        return new object[][]
        {
            // Canonical Moq pattern for non-generic EventHandler: Moq supplies the sender.
            ["""mock.Raise(n => n.Closed += null, EventArgs.Empty);"""],

            // A derived EventArgs is implicitly convertible to the base EventHandler parameter, so the
            // statically-known exact runtime type upcasts cleanly.
            ["""mock.Raise(n => n.Closed += null, new CustomEventArgs());"""],

            // Two-argument form binds Raise(..., params object[]) and is also legal.
            ["""mock.Raise(n => n.Closed += null, new object(), EventArgs.Empty);"""],

            // Two-argument form with a null-typed sender argument is legal (null binds to any reference).
            ["""mock.Raise(n => n.Closed += null, null, EventArgs.Empty);"""],

            // EventHandler<CustomEventArgs>: args-only form (existing behavior).
            ["""mock.Raise(n => n.CustomEvent += null, new CustomEventArgs());"""],

            // EventHandler<CustomEventArgs>: two-argument form (previously a false positive).
            ["""mock.Raise(n => n.CustomEvent += null, new object(), new CustomEventArgs());"""],

            // Custom (object sender, TArgs e) delegate: both forms are legal.
            ["""mock.Raise(n => n.ShapedEvent += null, new CustomEventArgs());"""],
            ["""mock.Raise(n => n.ShapedEvent += null, new object(), new CustomEventArgs());"""],

            // Invalid: payload not convertible to EventArgs.
            ["""mock.Raise(n => n.Closed += null, {|Moq1202:42|});"""],

            // Conditional access uses MemberBindingExpressionSyntax and must not be pre-filtered out.
            ["""mock?.Raise(n => n.Closed += null, {|Moq1202:42|});"""],

            // Invalid: object payload is only EXPLICITLY convertible to EventArgs. Overload resolution
            // binds the params-object array overload instead of the EventArgs overload, so Moq does not
            // supply the sender and the one-argument form throws at runtime and must be flagged.
            ["""mock.Raise(n => n.Closed += null, {|Moq1202:new object()|});"""],

            // Invalid: an unrelated EventArgs subclass has no conversion to the CustomEventArgs delegate
            // parameter, so Moq cannot bind it even though it selects the sender-supplying overload.
            ["""mock.Raise(n => n.CustomEvent += null, {|Moq1202:new OtherEventArgs()|});"""],

            // Invalid: EventArgs.Empty is a base EventArgs singleton. Moq selects the sender-supplying
            // overload, then casts the payload to CustomEventArgs at runtime and throws because the exact
            // runtime type is System.EventArgs. The exact type is statically known, so it is flagged.
            ["""mock.Raise(n => n.CustomEvent += null, {|Moq1202:EventArgs.Empty|});"""],

            // Invalid: a freshly constructed base EventArgs has a statically-known exact runtime type that
            // cannot be cast to the derived CustomEventArgs delegate parameter.
            ["""mock.Raise(n => n.CustomEvent += null, {|Moq1202:new EventArgs()|});"""],

            // Invalid: a user-defined explicit conversion exists to CustomEventArgs, but Moq's reflection
            // cast never invokes user-defined operators, so the freshly constructed SourceEventArgs throws.
            ["""mock.Raise(n => n.CustomEvent += null, {|Moq1202:new SourceEventArgs()|});"""],

            // Valid (tolerated): an EventArgs-typed local can hold a derived CustomEventArgs at runtime, so
            // the downcast is statically indistinguishable from a valid one. Moq accepts it, so no flag.
            ["""EventArgs downcastArgs = new CustomEventArgs(); mock.Raise(n => n.CustomEvent += null, downcastArgs);"""],

            // Valid (tolerated): a field reference other than EventArgs.Empty has an unknown exact runtime
            // type, so the downcast is tolerated exactly like the local-variable case above.
            ["""mock.Raise(n => n.CustomEvent += null, SharedArgs);"""],

            // Valid (tolerated): an explicit cast wraps an object-creation whose exact runtime type derives
            // from the delegate parameter, so the cast is unwrapped and the payload accepted.
            ["""mock.Raise(n => n.CustomEvent += null, (EventArgs)new CustomEventArgs());"""],

            // Invalid: an explicit cast wraps an object-creation whose exact runtime type is an unrelated
            // sibling. The cast is unwrapped and the payload flagged because it can never bind.
            ["""mock.Raise(n => n.CustomEvent += null, {|Moq1202:(EventArgs)new OtherEventArgs()|});"""],

            // Invalid: nested casts wrap a base EventArgs object-creation. All conversions are unwrapped to
            // reveal the exact runtime type, which cannot be cast to the derived delegate parameter.
            ["""mock.Raise(n => n.CustomEvent += null, {|Moq1202:(EventArgs)(object)new EventArgs()|});"""],

            // Invalid: an EventArgs-typed local declared as a type with only a user-defined conversion to
            // the delegate parameter. Moq's reflection cast never invokes user-defined operators, so it is
            // flagged even though its runtime type is unknown to the analyzer.
            ["""SourceEventArgs sourceArgs = new SourceEventArgs(); mock.Raise(n => n.CustomEvent += null, {|Moq1202:sourceArgs|});"""],

            // Valid: an explicit user-defined conversion is applied at the call site, so the operator runs and
            // constructs a genuine CustomEventArgs before Moq receives it. The conversion is not unwrapped
            // because it does not preserve object identity.
            ["""mock.Raise(n => n.CustomEvent += null, (CustomEventArgs)new SourceEventArgs());"""],

            // Valid: an implicit user-defined conversion to the delegate argument type is applied at the call
            // site. The operator runs and Moq receives a genuine CustomEventArgs, so no diagnostic is reported.
            ["""mock.Raise(n => n.CustomEvent += null, new ConvertibleToCustom());"""],

            // Invalid: an implicit user-defined conversion produces an unrelated sibling (OtherEventArgs). The
            // operator runs and Moq receives an OtherEventArgs, which cannot be cast to CustomEventArgs.
            ["""mock.Raise(n => n.CustomEvent += null, {|Moq1202:new ConvertibleToOther()|});"""],

            // Valid: a conditional expression whose runtime type is not statically known. The value is a
            // CustomEventArgs at runtime, so no diagnostic is reported (matches the unknown-runtime-type path).
            ["""mock.Raise(n => n.CustomEvent += null, true ? new CustomEventArgs() : new CustomEventArgs());"""],

            // Valid: a null-coalescing expression yields a CustomEventArgs. The runtime type is tolerated as a
            // reference or identity conversion to the delegate argument type, so no diagnostic is reported.
            ["""CustomEventArgs maybeArgs = new CustomEventArgs(); mock.Raise(n => n.CustomEvent += null, maybeArgs ?? new CustomEventArgs());"""],

            // Invalid: three arguments for a two-parameter delegate.
            ["""mock.Raise(n => n.Closed += null, new object(), EventArgs.Empty, {|Moq1202:"extra"|});"""],

            // Invalid: no arguments at all.
            ["""{|Moq1202:mock.Raise(n => n.Closed += null)|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> MultiParameterAndNestedGenericTestData()
    {
        return new object[][]
        {
            // Action<T1, T2> is resolved through the Invoke signature.
            ["""mock.Raise(n => n.PairChanged += null, 1, "two");"""],
            ["""mock.Raise(n => n.PairChanged += null, 1, {|Moq1202:2|});"""],

            // Nested generic payload.
            ["""mock.Raise(n => n.ItemsChanged += null, new List<Dictionary<string, int>>());"""],
            ["""mock.Raise(n => n.ItemsChanged += null, {|Moq1202:new List<int>()|});"""],

            // EventHandler<T> where T does not derive from EventArgs: full form required.
            ["""mock.Raise(n => n.StringHandler += null, new object(), "payload");"""],
            ["""{|Moq1202:mock.Raise(n => n.StringHandler += null, "payload")|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> UnresolvableDelegateTestData()
    {
        return new object[][]
        {
            // The delegate type does not resolve (mid-edit code): no Moq1202 must be reported.
            ["""mock.Raise(n => n.Broken += null, 42);"""],
            ["""mock.Raise(n => n.Broken += null, EventArgs.Empty, "extra");"""],
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
                  event Action<string, int> PairChanged;
                  event Action<List<string>> ListChanged;
              }

              internal class CustomRaiser
              {
                  public void Raise(Action<IOptionsProvider> selector, object value)
                  {
                  }
              }

              internal class UnitTest
              {
                  private static void Raise<T>()
                  {
                  }

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
                  event Action<string, int> PairChanged;
                  event Action<List<string>> ListChanged;
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
    [MemberData(nameof(MockAccessPatternTestData))]
    public async Task ShouldAnalyzeRaiseOnMemberAndMethodChainMockAccess(string referenceAssemblyGroup, string @namespace, string raiseCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              internal interface IOptionsProvider
              {
                  event Action<string> StringOptionsChanged;
              }

              internal class Wrapper
              {
                  public Mock<IOptionsProvider> ProviderMock { get; } = new Mock<IOptionsProvider>();

                  public Mock<IOptionsProvider> GetMock() => ProviderMock;
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var wrapper = new Wrapper();
                      {{raiseCall}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(InvalidTestData2))]
    public async Task ShouldHandleEventHandlerPattern(string referenceAssemblyGroup, string @namespace, string raiseCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{@namespace}}
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
                    {{raiseCall}}
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(EventHandlerShapedTestData))]
    public async Task ShouldHandleEventHandlerShapedDelegates(string referenceAssemblyGroup, string @namespace, string raiseCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}
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
                  event EventHandler Closed;
                  event EventHandler<CustomEventArgs> CustomEvent;
                  event ShapedEventHandler ShapedEvent;
              }

              internal class UnitTest
              {
                  private static readonly EventArgs SharedArgs = new CustomEventArgs();

                  private void Test()
                  {
                      var mock = new Mock<INotifier>();
                      {{raiseCall}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MultiParameterAndNestedGenericTestData))]
    public async Task ShouldHandleMultiParameterAndNestedGenericDelegates(string referenceAssemblyGroup, string @namespace, string raiseCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}
              using System;
              using System.Collections.Generic;

              internal interface INotifier
              {
                  event Action<int, string> PairChanged;
                  event Action<List<Dictionary<string, int>>> ItemsChanged;
                  event EventHandler<string> StringHandler;
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = new Mock<INotifier>();
                      {{raiseCall}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(UnresolvableDelegateTestData))]
    public async Task ShouldNotReportWhenEventDelegateTypeDoesNotResolve(string referenceAssemblyGroup, string @namespace, string raiseCall)
    {
        // CompilerDiagnostics.None suppresses CS0246 for the intentionally-unresolved delegate type,
        // mirroring the malformed-code pattern used in MockRepositoryVerifyAnalyzerTests.cs:327.
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}
              using System;

              internal interface IBrokenNotifier
              {
                  event UnresolvedDelegateType Broken;
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = new Mock<IBrokenNotifier>();
                      {{raiseCall}}
                  }
              }
              """,
            referenceAssemblyGroup,
            CompilerDiagnostics.None);
    }

    [Theory]
    [MemberData(nameof(UnresolvableDelegateTestData))]
    public async Task ShouldNotReportWhenEventTypeIsNotDelegate(string referenceAssemblyGroup, string @namespace, string raiseCall)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}
              using System;

              internal interface IBrokenNotifier
              {
                  event string Broken;
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = new Mock<IBrokenNotifier>();
                      {{raiseCall}}
                  }
              }
              """,
            referenceAssemblyGroup,
            CompilerDiagnostics.None);
    }
}
