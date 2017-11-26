using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moq.Analyzers
{
    class Diagnostics
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
    }
}
