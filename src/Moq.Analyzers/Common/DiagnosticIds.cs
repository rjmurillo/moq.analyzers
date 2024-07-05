namespace Moq.Analyzers.Common;

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
}
