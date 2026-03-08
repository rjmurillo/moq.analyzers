using Moq.Analyzers.Common.WellKnown;
using RaisesVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.RaisesEventArgumentsShouldMatchEventSignatureAnalyzer>;
using RaiseVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.RaiseEventArgumentsShouldMatchEventSignatureAnalyzer>;

namespace Moq.Analyzers.Test.Common;

public class EventSyntaxExtensionsTests
{
#pragma warning disable RS2008 // Enable analyzer release tracking (test-only descriptor)
#pragma warning disable ECS1300 // Test-only descriptor; inline init is simpler than static constructor
    private static readonly DiagnosticDescriptor TestRuleWithPlaceholder = new(
        "EVT0001",
        "Test",
        "Event '{0}' has wrong args",
        "Test",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TestRuleNoPlaceholder = new(
        "EVT0002",
        "Test",
        "Event has wrong args",
        "Test",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
#pragma warning restore ECS1300
#pragma warning restore RS2008

    public static IEnumerable<object[]> TooFewArgumentsData()
    {
        return new object[][]
        {
            // Action<string> expects 1 arg, Raises passes 0
            ["""{|Moq1204:mockProvider.Setup(x => x.Submit()).Raises(x => x.StringEvent += null)|};"""],

            // Custom delegate expects 2 params, Raises passes 1
            ["""{|Moq1204:mockProvider.Setup(x => x.Submit()).Raises(x => x.TwoParamDelegate += null, "only-one")|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> TooManyArgumentsData()
    {
        return new object[][]
        {
            // Action<string> expects 1 arg, Raises passes 3
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.StringEvent += null, "test", {|Moq1204:"extra1"|});"""],

            // Action event (no params) with extra arg
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.SimpleEvent += null, {|Moq1204:"unexpected"|});"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> WrongArgumentTypesData()
    {
        return new object[][]
        {
            // Action<string> event with int argument
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.StringEvent += null, {|Moq1204:42|});"""],

            // Action<int> event with string argument
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.NumberEvent += null, {|Moq1204:"wrong"|});"""],

            // EventHandler<CustomArgs> with wrong type
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.CustomEvent += null, {|Moq1204:"notCustomArgs"|});"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ExactMatchData()
    {
        return new object[][]
        {
            // Action<string> with correct string
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.StringEvent += null, "correct");"""],

            // Action<int> with correct int
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.NumberEvent += null, 42);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> EventHandlerSubclassData()
    {
        return new object[][]
        {
            // EventHandler<CustomArgs> with CustomArgs instance
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.CustomEvent += null, new CustomArgs());"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ActionDelegateData()
    {
        return new object[][]
        {
            // Action<double> with implicit int-to-double conversion
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.DoubleEvent += null, 42);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> CustomDelegateData()
    {
        return new object[][]
        {
            // Custom delegate with matching parameters
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.TwoParamDelegate += null, "hello", 99);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ZeroExpectedParametersData()
    {
        return new object[][]
        {
            // Action event with zero parameters, no args passed
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.SimpleEvent += null);"""],

            // Parameterless custom delegate with no args
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.NoParamDelegate += null);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ManyParametersMatchData()
    {
        return new object[][]
        {
            // Action<int, string, bool> with all correct types
            ["""mockProvider.Raise(p => p.MultiParamEvent += null, 1, "two", true);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ManyParametersMismatchData()
    {
        return new object[][]
        {
            // Action<int, string, bool> with third arg wrong type
            ["""mockProvider.Raise(p => p.MultiParamEvent += null, 1, "two", {|Moq1202:"notBool"|});"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ZeroParamsWithExtraArgsData()
    {
        return new object[][]
        {
            // Parameterless delegate, but extra args are passed
            ["""mockProvider.Setup(x => x.Submit()).Raises(x => x.NoParamDelegate += null, {|Moq1204:"extra"|});"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> RaiseWithEventNameData()
    {
        return new object[][]
        {
            // Raise (with event name) too few args
            ["""{|Moq1202:mockProvider.Raise(p => p.StringOptionsChanged += null)|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Fact]
    public void GetEventParameterTypes_MultiArgActionDelegate_FallsBackToCustomDelegateInvoke()
    {
        const string code = @"
using System;
class C
{
    event Action<int, string> MyEvent;
}";
        (ITypeSymbol eventType, KnownSymbols knownSymbols) = GetEventFieldTypeWithKnownSymbols(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType, knownSymbols);

        Assert.Equal(2, result.Length);
        Assert.Equal("int", result[0].ToDisplayString());
        Assert.Equal("string", result[1].ToDisplayString());
    }

    [Theory]
    [InlineData("Action<double>", "double")]
    [InlineData("EventHandler<EventArgs>", "System.EventArgs")]
    public void GetEventParameterTypes_SingleTypeArgDelegate_ReturnsSingleType(
        string delegateType,
        string expectedTypeName)
    {
        string code = $@"
using System;
class C
{{
    event {delegateType} MyEvent;
}}";
        (ITypeSymbol eventType, KnownSymbols knownSymbols) = GetEventFieldTypeWithKnownSymbols(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType, knownSymbols);

        Assert.Single(result);
        Assert.Equal(expectedTypeName, result[0].ToDisplayString());
    }

    [Fact]
    public void GetEventParameterTypes_CustomDelegate_ReturnsInvokeMethodParameters()
    {
        const string code = @"
delegate void MyDelegate(int x, bool y);
class C
{
    event MyDelegate MyEvent;
}";
        (ITypeSymbol eventType, KnownSymbols knownSymbols) = GetEventFieldTypeWithKnownSymbols(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType, knownSymbols);

        Assert.Equal(2, result.Length);
        Assert.Equal("int", result[0].ToDisplayString());
        Assert.Equal("bool", result[1].ToDisplayString());
    }

    [Fact]
    public void GetEventParameterTypes_CustomDelegateNoParameters_ReturnsEmpty()
    {
        const string code = @"
delegate void MyDelegate();
class C
{
    event MyDelegate MyEvent;
}";
        (ITypeSymbol eventType, KnownSymbols knownSymbols) = GetEventFieldTypeWithKnownSymbols(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType, knownSymbols);

        Assert.Empty(result);
    }

    [Fact]
    public void GetEventParameterTypes_NonNamedTypeSymbol_ReturnsEmpty()
    {
        const string code = @"
class C
{
    int[] Field;
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        VariableDeclaratorSyntax fieldSyntax = tree.GetRoot()
            .DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        IFieldSymbol field = (IFieldSymbol)model.GetDeclaredSymbol(fieldSyntax)!;
        ITypeSymbol arrayType = field.Type;

        Assert.IsNotAssignableFrom<INamedTypeSymbol>(arrayType);

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(arrayType, knownSymbols);

        Assert.Empty(result);
    }

    [Fact]
    public void GetEventParameterTypes_PlainEventHandler_ReturnsFallbackInvokeParams()
    {
        const string code = @"
using System;
class C
{
    event EventHandler MyEvent;
}";
        (ITypeSymbol eventType, KnownSymbols knownSymbols) = GetEventFieldTypeWithKnownSymbols(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType, knownSymbols);

        Assert.Equal(2, result.Length);
        Assert.Contains("object", result[0].ToDisplayString(), StringComparison.Ordinal);
        Assert.Contains("EventArgs", result[1].ToDisplayString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TryGetEventMethodArguments_NoArguments_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        SomeMethod();
    }
    void SomeMethod() {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, null),
            knownSymbols);

        Assert.False(result);
        Assert.Empty(eventArguments);
        Assert.Empty(expectedParameterTypes);
    }

    [Fact]
    public void TryGetEventMethodArguments_ExtractorReturnsFalse_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        SomeMethod(42);
    }
    void SomeMethod(int x) {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (false, null),
            knownSymbols);

        Assert.False(result);
        Assert.Empty(eventArguments);
        Assert.Empty(expectedParameterTypes);
    }

    [Fact]
    public void TryGetEventMethodArguments_ExtractorReturnsNullType_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        SomeMethod(42);
    }
    void SomeMethod(int x) {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, null),
            knownSymbols);

        Assert.False(result);
        Assert.Empty(eventArguments);
        Assert.Empty(expectedParameterTypes);
    }

    [Fact]
    public void TryGetEventMethodArguments_OnlyEventSelector_ReturnsTrueWithEmptyArgs()
    {
        const string code = @"
using System;
delegate void MyDelegate(int x);
class C
{
    event MyDelegate MyEvent;
    void M()
    {
        SomeMethod(0);
    }
    void SomeMethod(int selector) {}
}";
        (ITypeSymbol delegateType, KnownSymbols _) = GetEventFieldTypeWithKnownSymbols(
            @"delegate void MyDelegate(int x);
class C { event MyDelegate MyEvent; }",
            "MyEvent");

        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, delegateType),
            knownSymbols);

        Assert.True(result);
        Assert.Empty(eventArguments);
        Assert.Single(expectedParameterTypes);
        Assert.Equal("int", expectedParameterTypes[0].ToDisplayString());
    }

    [Fact]
    public void TryGetEventMethodArguments_WithAdditionalArgs_ReturnsTrueWithArgs()
    {
        const string code = @"
using System;
class C
{
    void M()
    {
        SomeMethod(0, 42, ""hello"");
    }
    void SomeMethod(int selector, int a, string b) {}
}";
        (ITypeSymbol delegateType, KnownSymbols _) = GetEventFieldTypeWithKnownSymbols(
            @"delegate void MyDelegate(int x, string y);
class C { event MyDelegate MyEvent; }",
            "MyEvent");

        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, delegateType),
            knownSymbols);

        Assert.True(result);
        Assert.Equal(2, eventArguments.Length);
        Assert.Equal(2, expectedParameterTypes.Length);
    }

    [Fact]
    public void CreateEventDiagnostic_WithEventName_SetsRuleLocationAndMessage()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { }");
        Location location = tree.GetRoot().GetLocation();

        Diagnostic diagnostic = EventSyntaxExtensions.CreateEventDiagnostic(location, TestRuleWithPlaceholder, "MyEvent");

        Assert.Equal(TestRuleWithPlaceholder.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("MyEvent", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Equal(location.SourceSpan, diagnostic.Location.SourceSpan);
    }

    [Fact]
    public void CreateEventDiagnostic_WithNullEventName_SetsRuleLocationAndMessage()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { }");
        Location location = tree.GetRoot().GetLocation();

        Diagnostic diagnostic = EventSyntaxExtensions.CreateEventDiagnostic(location, TestRuleNoPlaceholder, null);

        Assert.Equal(TestRuleNoPlaceholder.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("Event has wrong args", diagnostic.GetMessage());
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Equal(location.SourceSpan, diagnostic.Location.SourceSpan);
    }

    [Fact]
    public void CreateEventDiagnostic_WithEmptyStringEventName_PassesEmptyStringAsArg()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { }");
        Location location = tree.GetRoot().GetLocation();

        Diagnostic diagnostic = EventSyntaxExtensions.CreateEventDiagnostic(location, TestRuleWithPlaceholder, string.Empty);

        Assert.Equal("EVT0001", diagnostic.Id);
        Assert.Equal("Event '' has wrong args", diagnostic.GetMessage());
    }

    [Fact]
    public void CreateEventDiagnostic_WithSpecialCharactersInEventName_PassesNameAsArg()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { }");
        Location location = tree.GetRoot().GetLocation();

        Diagnostic diagnostic = EventSyntaxExtensions.CreateEventDiagnostic(location, TestRuleWithPlaceholder, "On<Click>");

        Assert.Equal("EVT0001", diagnostic.Id);
        Assert.Contains("On<Click>", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    // ValidateEventArgumentTypes tests exercise the method indirectly through the
    // analyzer pipeline. SyntaxNodeAnalysisContext has no public constructor, so direct
    // unit testing is not feasible.
    [Theory]
    [MemberData(nameof(TooFewArgumentsData))]
    [MemberData(nameof(TooManyArgumentsData))]
    [MemberData(nameof(WrongArgumentTypesData))]
    [MemberData(nameof(ExactMatchData))]
    [MemberData(nameof(EventHandlerSubclassData))]
    [MemberData(nameof(ActionDelegateData))]
    [MemberData(nameof(CustomDelegateData))]
    [MemberData(nameof(ZeroExpectedParametersData))]
    [MemberData(nameof(ZeroParamsWithExtraArgsData))]
    public async Task ValidateEventArgumentTypes_RaisesScenarios_VerifiesDiagnostics(
        string referenceAssemblyGroup,
        string @namespace,
        string raisesCall)
    {
        await RaisesVerifier.VerifyAnalyzerAsync(
            CreateRaisesTestCode(@namespace, raisesCall),
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(ManyParametersMatchData))]
    [MemberData(nameof(ManyParametersMismatchData))]
    [MemberData(nameof(RaiseWithEventNameData))]
    public async Task ValidateEventArgumentTypes_RaiseScenarios_VerifiesDiagnostics(
        string referenceAssemblyGroup,
        string @namespace,
        string raiseCall)
    {
        await RaiseVerifier.VerifyAnalyzerAsync(
            CreateRaiseTestCode(@namespace, raiseCall),
            referenceAssemblyGroup);
    }

    private static string CreateRaisesTestCode(string @namespace, string raisesCall)
    {
        return $$"""
            {{@namespace}}
            using Moq;
            using System;

            internal class CustomArgs : EventArgs
            {
                public string Value { get; set; }
            }

            internal delegate void TwoParamDelegate(string a, int b);

            internal delegate void NoParamDelegate();

            internal interface ITestInterface
            {
                void Submit();
                event Action<string> StringEvent;
                event Action<int> NumberEvent;
                event Action<double> DoubleEvent;
                event EventHandler<CustomArgs> CustomEvent;
                event Action SimpleEvent;
                event TwoParamDelegate TwoParamDelegate;
                event NoParamDelegate NoParamDelegate;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<ITestInterface>();
                    {{raisesCall}}
                }
            }
            """;
    }

    private static string CreateRaiseTestCode(string @namespace, string raiseCall)
    {
        return $$"""
            {{@namespace}}
            using Moq;
            using System;

            internal class MyOptions { }
            internal class Incorrect { }

            internal interface IOptionsProvider
            {
                event Action<string> StringOptionsChanged;
                event Action<int> NumberChanged;
                event Action<MyOptions> OptionsChanged;
                event Action SimpleEvent;
                event Action<double> DoubleChanged;
                event Action<int, string, bool> MultiParamEvent;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mockProvider = new Mock<IOptionsProvider>();
                    {{raiseCall}}
                }
            }
            """;
    }

#pragma warning disable ECS0900 // Boxing needed to cast to IEventSymbol from GetDeclaredSymbol
    private static (ITypeSymbol EventType, KnownSymbols KnownSymbols) GetEventFieldTypeWithKnownSymbols(
        string code,
        string eventName)
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        VariableDeclaratorSyntax variable = tree.GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .First(v => v.Parent?.Parent is EventFieldDeclarationSyntax &&
                        string.Equals(v.Identifier.Text, eventName, StringComparison.Ordinal));
        IEventSymbol eventSymbol = (IEventSymbol)model.GetDeclaredSymbol(variable)!;
        return (eventSymbol.Type, knownSymbols);
    }
#pragma warning restore ECS0900
}
