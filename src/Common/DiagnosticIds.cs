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
    internal const string AsShouldOnlyBeUsedForInterfacesRuleId = "Moq1300";
    internal const string SetExplicitMockBehavior = "Moq1400";
    internal const string SetStrictMockBehavior = "Moq1410";
    internal const string VerifyOnlyUsedForOverridableMembers = "Moq1500";
    internal const string MockOfComplexExpression = "Moq1600";
    internal const string SequenceSetupAfterStandardSetup = "Moq1700";
    internal const string EventHandlerSignatureMismatch = "Moq1800";
    internal const string MockRepositoryVerifyMissing = "Moq1900";
}
