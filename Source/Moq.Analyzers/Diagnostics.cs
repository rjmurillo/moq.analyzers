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
        internal const string NoSealedClassMocksTitle = "Moq: Cannot mock sealed class";
        internal const string NoSealedClassMocksMessage = "Sealed classes cannot be mocked.";

        internal const string NoConstructorArgumentsForInterfaceMockId = "Moq1001";
        internal const string NoConstructorArgumentsForInterfaceMockTitle = "Moq: Parameters for mocked interface";
        internal const string NoConstructorArgumentsForInterfaceMockMessage = "Do not specify parameters for mocked interface.";

        internal const string ConstructorArgumentsShouldMatchId = "Moq1002";
        internal const string ConstructorArgumentsShouldMatchTitle = "Moq: No constructors with such parameters";
        internal const string ConstructorArgumentsShouldMatchMessage = "Parameters provided into mock do not match existing constructors.";

        internal const string CallbackSignatureShouldMatchMockedMethodId = "Moq1100";
        internal const string CallbackSignatureShouldMatchMockedMethodTitle = "Moq: Bad callback signature";
        internal const string CallbackSignatureShouldMatchMockedMethodMessage = "Callback must have the same signature as the mocked method.";

        internal const string NoMethodsInPropertySetupId = "Moq1101";
        internal const string NoMethodsInPropertySetupTitle = "Moq: Method is referenced in property setup";
        internal const string NoMethodsInPropertySetupMessage = "Do not use SetupGet/SetupSet for methods.";
    }
}
