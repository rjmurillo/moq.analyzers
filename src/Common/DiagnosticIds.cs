namespace Moq.Analyzers.Common;

#pragma warning disable ECS0200 // Consider using readonly instead of const for flexibility

internal static class DiagnosticIds
{
    internal const string SealedClassCannotBeMocked = "Moq1000";
    internal const string NoConstructorArgumentsForInterfaceMockRuleId = "Moq1001";
    internal const string NoMatchingConstructorRuleId = "Moq1002";
    internal const string BadCallbackParameters = "Moq1100";
    internal const string PropertySetupUsedForMethod = "Moq1101";
    internal const string SetupOnlyUsedForOverridableMembers = "Moq1200";
    internal const string AsyncUsesReturnsAsyncInsteadOfResult = "Moq1201";
    internal const string RaiseEventArgumentsShouldMatchEventSignature = "Moq1202";
    internal const string MethodSetupShouldSpecifyReturnValue = "Moq1203";
    internal const string RaisesEventArgumentsShouldMatchEventSignature = "Moq1204";
    internal const string EventSetupHandlerShouldMatchEventType = "Moq1205";
    internal const string ReturnsAsyncShouldBeUsedForAsyncMethods = "Moq1206";
    internal const string VerifyOnlyUsedForOverridableMembers = "Moq1210";
    internal const string AsShouldOnlyBeUsedForInterfacesRuleId = "Moq1300";
    internal const string MockGetShouldNotTakeLiterals = "Moq1301";
    internal const string LinqToMocksExpressionShouldBeValid = "Moq1302";
    internal const string SetExplicitMockBehavior = "Moq1400";
    internal const string SetStrictMockBehavior = "Moq1410";
    internal const string MockRepositoryVerifyNotCalled = "Moq1500";
    internal const string SetupSequenceOnlyUsedForOverridableMembers = "Moq1800";
}
