using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Common;

namespace Moq.Analyzers.Test;

public class DiagnosticCategoryTests
{
    public static TheoryData<DiagnosticAnalyzer, string, string> UsageAnalyzers =>
        new()
        {
            { new NoSealedClassMocksAnalyzer(), "Moq1000", DiagnosticCategory.Usage },
            { new ConstructorArgumentsShouldMatchAnalyzer(), "Moq1001", DiagnosticCategory.Usage },
            { new ConstructorArgumentsShouldMatchAnalyzer(), "Moq1002", DiagnosticCategory.Usage },
            { new InternalTypeMustHaveInternalsVisibleToAnalyzer(), "Moq1003", DiagnosticCategory.Usage },
            { new NoMockOfLoggerAnalyzer(), "Moq1004", DiagnosticCategory.Usage },
            { new AsShouldBeUsedOnlyForInterfaceAnalyzer(), "Moq1300", DiagnosticCategory.Usage },
            { new MockGetShouldNotTakeLiteralsAnalyzer(), "Moq1301", DiagnosticCategory.Usage },
            { new LinqToMocksExpressionShouldBeValidAnalyzer(), "Moq1302", DiagnosticCategory.Usage },
            { new RedundantTimesSpecificationAnalyzer(), "Moq1420", DiagnosticCategory.Usage },
        };

    public static TheoryData<DiagnosticAnalyzer, string, string> CorrectnessAnalyzers =>
        new()
        {
            { new CallbackSignatureShouldMatchMockedMethodAnalyzer(), "Moq1100", DiagnosticCategory.Correctness },
            { new NoMethodsInPropertySetupAnalyzer(), "Moq1101", DiagnosticCategory.Correctness },
            { new SetupShouldBeUsedOnlyForOverridableMembersAnalyzer(), "Moq1200", DiagnosticCategory.Correctness },
            { new SetupShouldNotIncludeAsyncResultAnalyzer(), "Moq1201", DiagnosticCategory.Correctness },
            { new RaiseEventArgumentsShouldMatchEventSignatureAnalyzer(), "Moq1202", DiagnosticCategory.Correctness },
            { new MethodSetupShouldSpecifyReturnValueAnalyzer(), "Moq1203", DiagnosticCategory.Correctness },
            { new RaisesEventArgumentsShouldMatchEventSignatureAnalyzer(), "Moq1204", DiagnosticCategory.Correctness },
            { new EventSetupHandlerShouldMatchEventTypeAnalyzer(), "Moq1205", DiagnosticCategory.Correctness },
            { new ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer(), "Moq1206", DiagnosticCategory.Correctness },
            { new SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer(), "Moq1207", DiagnosticCategory.Correctness },
            { new ReturnsDelegateShouldReturnTaskAnalyzer(), "Moq1208", DiagnosticCategory.Correctness },
            { new VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer(), "Moq1210", DiagnosticCategory.Correctness },
        };

    public static TheoryData<DiagnosticAnalyzer, string, string> BestPracticeAnalyzers =>
        new()
        {
            { new SetExplicitMockBehaviorAnalyzer(), "Moq1400", DiagnosticCategory.BestPractice },
            { new SetStrictMockBehaviorAnalyzer(), "Moq1410", DiagnosticCategory.BestPractice },
            { new MockRepositoryVerifyAnalyzer(), "Moq1500", DiagnosticCategory.BestPractice },
        };

    [Theory]
    [MemberData(nameof(UsageAnalyzers))]
    [MemberData(nameof(CorrectnessAnalyzers))]
    [MemberData(nameof(BestPracticeAnalyzers))]
    public void Analyzer_ShouldReportExpectedCategory(DiagnosticAnalyzer analyzer, string expectedRuleId, string expectedCategory)
    {
        DiagnosticDescriptor? descriptor = analyzer.SupportedDiagnostics
            .FirstOrDefault(d => string.Equals(d.Id, expectedRuleId, StringComparison.Ordinal));

        Assert.NotNull(descriptor);
        Assert.Equal(expectedCategory, descriptor.Category);
    }
}
