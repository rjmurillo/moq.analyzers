using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test;

public class DiagnosticMessageTests
{
    public static TheoryData<Type, string, DiagnosticSeverity, string, string, string> FormattedDiagnosticMessageData()
    {
        TheoryData<Type, string, DiagnosticSeverity, string, string, string> data = [];

        AddMessageCase<NoSealedClassMocksAnalyzer>(
            data,
            DiagnosticIds.SealedClassCannotBeMocked,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal sealed class FooSealed { }", "new Mock<{|#0:FooSealed|}>();"),
            "Sealed class 'FooSealed' cannot be mocked");

        AddMessageCase<ConstructorArgumentsShouldMatchAnalyzer>(
            data,
            DiagnosticIds.NoConstructorArgumentsForInterfaceMockRuleId,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { }", "new Mock<IService>{|#0:(\"value\")|};"),
            "Mocked interface 'IService' cannot have constructor parameters (\"value\")");

        AddMessageCase<ConstructorArgumentsShouldMatchAnalyzer>(
            data,
            DiagnosticIds.NoMatchingConstructorRuleId,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal class Service { public Service(string value) { } }", "new Mock<Service>{|#0:(42)|};"),
            "Could not find a matching constructor for type 'Service' with arguments (42)");

        AddMessageCase<InternalTypeMustHaveInternalsVisibleToAnalyzer>(
            data,
            DiagnosticIds.InternalTypeMustHaveInternalsVisibleTo,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal class InternalService { public virtual void Run() { } }", "new Mock<{|#0:InternalService|}>();"),
            "Internal type 'InternalService' requires [InternalsVisibleTo(\"DynamicProxyGenAssembly2\")] in its assembly to be mocked");

        AddMessageCase<NoMockOfLoggerAnalyzer>(
            data,
            DiagnosticIds.LoggerShouldNotBeMocked,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging,
            CreateSource("using Microsoft.Extensions.Logging;", "new Mock<{|#0:ILogger|}>();"),
            "ILogger should not be mocked; use NullLogger.Instance or FakeLogger from Microsoft.Extensions.Diagnostics.Testing instead");

        AddMessageCase<CallbackSignatureShouldMatchMockedMethodAnalyzer>(
            data,
            DiagnosticIds.BadCallbackParameters,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { void Do(string value); }", "new Mock<IService>().Setup(x => x.Do(\"value\")).Callback(({|#0:int wrong|}) => { });"),
            "Callback signature for 'Do' must match the signature of the mocked method");

        AddMessageCase<NoMethodsInPropertySetupAnalyzer>(
            data,
            DiagnosticIds.PropertySetupUsedForMethod,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { int Get(); }", "new Mock<IService>().SetupGet(x => {|#0:x.Get()|});"),
            "SetupGet/SetupSet/SetupProperty should be used for properties, not for methods like 'Get'");

        AddMessageCase<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>(
            data,
            DiagnosticIds.SetupOnlyUsedForOverridableMembers,
            DiagnosticSeverity.Error,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal class Service { public int Get() => 0; }", "{|#0:new Mock<Service>().Setup(x => x.Get())|};"),
            "Setup should be used only for overridable members, but 'Service.Get()' is not overridable");

        AddMessageCase<SetupShouldNotIncludeAsyncResultAnalyzer>(
            data,
            DiagnosticIds.AsyncUsesReturnsAsyncInsteadOfResult,
            DiagnosticSeverity.Error,
            ReferenceAssemblyCatalog.Net80WithOldMoq,
            CreateSource("internal class Service { public virtual Task<int> GetAsync() => Task.FromResult(0); }", "new Mock<Service>().Setup(x => {|#0:x.GetAsync().Result|});"),
            "Setup of async method 'GetAsync' should use ReturnsAsync instead of .Result");

        AddMessageCase<RaiseEventArgumentsShouldMatchEventSignatureAnalyzer>(
            data,
            DiagnosticIds.RaiseEventArgumentsShouldMatchEventSignature,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { event EventHandler Closed; }", "new Mock<IService>().Raise(x => x.Closed += null, {|#0:42|});"),
            "Raise event arguments should match the 'IService.Closed' event delegate signature");

        AddMessageCase<MethodSetupShouldSpecifyReturnValueAnalyzer>(
            data,
            DiagnosticIds.MethodSetupShouldSpecifyReturnValue,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { int Get(); }", "{|#0:new Mock<IService>().Setup(x => x.Get())|};"),
            "Method setup for 'IService.Get()' should specify a return value");

        AddMessageCase<RaisesEventArgumentsShouldMatchEventSignatureAnalyzer>(
            data,
            DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { event Action<string> Changed; void Submit(); }", "new Mock<IService>().Setup(x => x.Submit()).Raises(x => x.Changed += null, {|#0:42|});"),
            "Raises event arguments should match the 'IService.Changed' event delegate signature");

        AddMessageCase<ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer>(
            data,
            DiagnosticIds.ReturnsAsyncShouldBeUsedForAsyncMethods,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal class Service { public virtual Task<string> GetAsync() => Task.FromResult(string.Empty); }", "new Mock<Service>().Setup(x => x.GetAsync()).{|#0:Returns(async () => \"value\")|};"),
            "Async method 'GetAsync' setups should use ReturnsAsync instead of Returns with async lambda");

        AddMessageCase<SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer>(
            data,
            DiagnosticIds.SetupSequenceOnlyUsedForOverridableMembers,
            DiagnosticSeverity.Error,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal class Service { public int Get() => 0; }", "new Mock<Service>().SetupSequence(x => {|#0:x.Get()|});"),
            "SetupSequence should be used only for overridable members, but 'Service.Get()' is not overridable");

        AddMessageCase<ReturnsDelegateShouldReturnTaskAnalyzer>(
            data,
            DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal class Service { public virtual Task<int> GetAsync() => Task.FromResult(0); }", "new Mock<Service>().Setup(x => x.GetAsync()).{|#0:Returns(() => 42)|};"),
            "Returns() delegate for async method 'GetAsync' should return 'Task<int>', not 'int'. Use ReturnsAsync() or wrap with Task.FromResult().");

        AddMessageCase<VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer>(
            data,
            DiagnosticIds.VerifyOnlyUsedForOverridableMembers,
            DiagnosticSeverity.Error,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal class Service { public void Do() { } }", "{|#0:new Mock<Service>().Verify(x => x.Do())|};"),
            "Verify should be used only for overridable members, but 'Do' is not overridable");

        AddMessageCase<AsShouldBeUsedOnlyForInterfaceAnalyzer>(
            data,
            DiagnosticIds.AsShouldOnlyBeUsedForInterfacesRuleId,
            DiagnosticSeverity.Error,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal class Service { }", "new Mock<object>().As<{|#0:Service|}>();"),
            "Mock.As() should take interfaces only, but 'Service' is not an interface");

        AddMessageCase<MockGetShouldNotTakeLiteralsAnalyzer>(
            data,
            DiagnosticIds.MockGetShouldNotTakeLiterals,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource(string.Empty, "Mock.Get({|#0:\"literal\"|});"),
            "Mock.Get() should not take literal 'literal'");

        AddMessageCase<LinqToMocksExpressionShouldBeValidAnalyzer>(
            data,
            DiagnosticIds.LinqToMocksExpressionShouldBeValid,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal class Service { public string Name { get; set; } = string.Empty; }", "Mock.Of<Service>(x => {|#0:x.Name|} == \"value\");"),
            "Invalid member 'x.Name' in LINQ to Mocks expression");

        AddMessageCase<SetExplicitMockBehaviorAnalyzer>(
            data,
            DiagnosticIds.SetExplicitMockBehavior,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { }", "{|#0:new Mock<IService>()|};"),
            "Explicitly choose a mocking behavior for IService instead of relying on the default (Loose) behavior");

        AddMessageCase<SetStrictMockBehaviorAnalyzer>(
            data,
            DiagnosticIds.SetStrictMockBehavior,
            DiagnosticSeverity.Info,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { }", "{|#0:new Mock<IService>(MockBehavior.Loose)|};"),
            "Explicitly set the Strict mocking behavior for 'IService'");

        AddMessageCase<RedundantTimesSpecificationAnalyzer>(
            data,
            DiagnosticIds.RedundantTimesSpecification,
            DiagnosticSeverity.Info,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { void Do(); }", "new Mock<IService>().Verify(x => x.Do(), {|#0:Times.AtLeastOnce()|});"),
            "Redundant Times.AtLeastOnce() specification can be removed as it is the default for Verify calls");

        AddMessageCase<MockRepositoryVerifyAnalyzer>(
            data,
            DiagnosticIds.MockRepositoryVerifyNotCalled,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("internal interface IService { }", "var {|#0:repository|} = new MockRepository(MockBehavior.Strict); repository.Create<IService>();"),
            "MockRepository 'repository' should have Verify() called");

        AddMessageCase<ProtectedSetupShouldUseItExprAnalyzer>(
            data,
            DiagnosticIds.ProtectedSetupUsesItMatcherInsteadOfItExpr,
            DiagnosticSeverity.Warning,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CreateSource("using Moq.Protected; internal abstract class Service { protected virtual bool Foo(string value) => true; }", "new Mock<Service>().Protected().Setup<bool>(\"Foo\", {|#0:It.IsAny<string>()|}).Returns(true);"),
            "Protected member setup uses 'It.IsAny' which is not compatible with string-based overloads; use an ItExpr matcher instead");

        return data;
    }

    [Theory]
    [MemberData(nameof(FormattedDiagnosticMessageData))]
    public async Task AnalyzerShouldReportFormattedDiagnosticMessage(
        Type analyzerType,
        string ruleId,
        DiagnosticSeverity severity,
        string referenceAssemblyGroup,
        string source,
        string expectedMessage)
    {
        DiagnosticResult expected = new DiagnosticResult(ruleId, severity)
            .WithLocation(0)
            .WithMessage(expectedMessage);

        await VerifyAnalyzerAsync(analyzerType, source, referenceAssemblyGroup, expected);
    }

    [Fact]
    public void FormattedDiagnosticMessageDataShouldCoverEveryReportableParameterizedDescriptor()
    {
        ImmutableHashSet<string> coveredRuleIds = FormattedDiagnosticMessageData()
            .Select(static row => (string)row[1])
            .ToImmutableHashSet(StringComparer.Ordinal);

        string[] uncoveredRuleIds = DiagnosticDescriptorMetadataTests.DiscoverDescriptors()
            .Where(static descriptor => descriptor.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains('{', StringComparison.Ordinal))
            .Where(descriptor => !coveredRuleIds.Contains(descriptor.Id))
            .Where(static descriptor => !IsCompilerBoundEventSetupDiagnostic(descriptor.Id))
            .Select(static descriptor => descriptor.Id)
            .ToArray();

        Assert.Empty(uncoveredRuleIds);
    }

    private static void AddMessageCase<TAnalyzer>(
        TheoryData<Type, string, DiagnosticSeverity, string, string, string> data,
        string ruleId,
        DiagnosticSeverity severity,
        string referenceAssemblyGroup,
        string source,
        string expectedMessage)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        data.Add(typeof(TAnalyzer), ruleId, severity, referenceAssemblyGroup, source, expectedMessage);
    }

    private static bool IsCompilerBoundEventSetupDiagnostic(string ruleId)
    {
        // Moq1205 mismatches are compiler errors before the analyzer can report a diagnostic.
        return string.Equals(ruleId, DiagnosticIds.EventSetupHandlerShouldMatchEventType, StringComparison.Ordinal);
    }

    private static string CreateSource(string declarations, string testCode)
    {
        return $$"""
            {{declarations}}

            internal class UnitTest
            {
                private void Test()
                {
                    {{testCode}}
                }
            }
            """;
    }

    private static Task VerifyAnalyzerAsync(
        Type analyzerType,
        string source,
        string referenceAssemblyGroup,
        DiagnosticResult expected)
    {
        return analyzerType.Name switch
        {
            nameof(NoSealedClassMocksAnalyzer) => VerifyAnalyzerAsync<NoSealedClassMocksAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(ConstructorArgumentsShouldMatchAnalyzer) => VerifyAnalyzerAsync<ConstructorArgumentsShouldMatchAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(InternalTypeMustHaveInternalsVisibleToAnalyzer) => VerifyAnalyzerAsync<InternalTypeMustHaveInternalsVisibleToAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(NoMockOfLoggerAnalyzer) => VerifyAnalyzerAsync<NoMockOfLoggerAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(CallbackSignatureShouldMatchMockedMethodAnalyzer) => VerifyAnalyzerAsync<CallbackSignatureShouldMatchMockedMethodAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(NoMethodsInPropertySetupAnalyzer) => VerifyAnalyzerAsync<NoMethodsInPropertySetupAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(SetupShouldBeUsedOnlyForOverridableMembersAnalyzer) => VerifyAnalyzerAsync<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(SetupShouldNotIncludeAsyncResultAnalyzer) => VerifyAnalyzerAsync<SetupShouldNotIncludeAsyncResultAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(RaiseEventArgumentsShouldMatchEventSignatureAnalyzer) => VerifyAnalyzerAsync<RaiseEventArgumentsShouldMatchEventSignatureAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(MethodSetupShouldSpecifyReturnValueAnalyzer) => VerifyAnalyzerAsync<MethodSetupShouldSpecifyReturnValueAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(RaisesEventArgumentsShouldMatchEventSignatureAnalyzer) => VerifyAnalyzerAsync<RaisesEventArgumentsShouldMatchEventSignatureAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer) => VerifyAnalyzerAsync<ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer) => VerifyAnalyzerAsync<SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(ReturnsDelegateShouldReturnTaskAnalyzer) => VerifyAnalyzerAsync<ReturnsDelegateShouldReturnTaskAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer) => VerifyAnalyzerAsync<VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(AsShouldBeUsedOnlyForInterfaceAnalyzer) => VerifyAnalyzerAsync<AsShouldBeUsedOnlyForInterfaceAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(MockGetShouldNotTakeLiteralsAnalyzer) => VerifyAnalyzerAsync<MockGetShouldNotTakeLiteralsAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(LinqToMocksExpressionShouldBeValidAnalyzer) => VerifyAnalyzerAsync<LinqToMocksExpressionShouldBeValidAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(SetExplicitMockBehaviorAnalyzer) => VerifyAnalyzerAsync<SetExplicitMockBehaviorAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(SetStrictMockBehaviorAnalyzer) => VerifyAnalyzerAsync<SetStrictMockBehaviorAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(RedundantTimesSpecificationAnalyzer) => VerifyAnalyzerAsync<RedundantTimesSpecificationAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(MockRepositoryVerifyAnalyzer) => VerifyAnalyzerAsync<MockRepositoryVerifyAnalyzer>(source, referenceAssemblyGroup, expected),
            nameof(ProtectedSetupShouldUseItExprAnalyzer) => VerifyAnalyzerAsync<ProtectedSetupShouldUseItExprAnalyzer>(source, referenceAssemblyGroup, expected),
            _ => throw new InvalidOperationException($"No verifier registered for {analyzerType.Name}."),
        };
    }

    private static Task VerifyAnalyzerAsync<TAnalyzer>(
        string source,
        string referenceAssemblyGroup,
        DiagnosticResult expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        return AnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, expected);
    }
}
