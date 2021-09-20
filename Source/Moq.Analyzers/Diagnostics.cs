namespace Moq.Analyzers
{
    internal static class Diagnostics
    {
        internal const string Category = "Moq";

        internal const string NoSealedClassMocksId = "Moq1000";
        internal const string NoSealedClassMocksTitle = "Moq: Sealed class mocked";
        internal const string NoSealedClassMocksMessage = "Sealed classes cannot be mocked.";

        internal const string NoConstructorArgumentsForInterfaceMockId = "Moq1001";
        internal const string NoConstructorArgumentsForInterfaceMockTitle = "Moq: Parameters specified for mocked interface";
        internal const string NoConstructorArgumentsForInterfaceMockMessage = "Mocked interfaces cannot have constructor parameters.";

        internal const string ConstructorArgumentsShouldMatchId = "Moq1002";
        internal const string ConstructorArgumentsShouldMatchTitle = "Moq: No matching constructor";
        internal const string ConstructorArgumentsShouldMatchMessage = "Parameters provided into mock do not match any existing constructors.";

        internal const string CallbackSignatureShouldMatchMockedMethodId = "Moq1100";
        internal const string CallbackSignatureShouldMatchMockedMethodTitle = "Moq: Bad callback parameters";
        internal const string CallbackSignatureShouldMatchMockedMethodMessage = "Callback signature must match the signature of the mocked method.";

        internal const string NoMethodsInPropertySetupId = "Moq1101";
        internal const string NoMethodsInPropertySetupTitle = "Moq: Property setup used for a method";
        internal const string NoMethodsInPropertySetupMessage = "SetupGet/SetupSet should be used for properties, not for methods.";


        internal const string SetupShouldBeUsedOnlyForOverridableMembersId = "Moq1200";
        internal const string SetupShouldBeUsedOnlyForOverridableMembersTitle = "Moq: Invalid setup parameter";
        internal const string SetupShouldBeUsedOnlyForOverridableMembersMessage = "Setup should be used only for overridable members.";

        internal const string SetupShouldNotIncludeAsyncResultId = "Moq1201";
        internal const string SetupShouldNotIncludeAsyncResultTitle = SetupShouldBeUsedOnlyForOverridableMembersTitle;
        internal const string SetupShouldNotIncludeAsyncResultMessage = "Setup of async methods should use ReturnsAsync instead of .Result";

        internal const string AsShouldBeUsedOnlyForInterfaceId = "Moq1300";
        internal const string AsShouldBeUsedOnlyForInterfaceTitle = "Moq: Invalid As type parameter";
        internal const string AsShouldBeUsedOnlyForInterfaceMessage = "Mock.As() should take interfaces only";
    }
}
