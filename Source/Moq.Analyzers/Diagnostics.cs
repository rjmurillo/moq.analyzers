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

        internal const string ConstructorParametersForInterfaceId = "Moq1001";
        internal const string ConstructorParametersForInterfaceTitle = "Moq: Parameters for mocked interface";
        internal const string ConstructorParametersForInterfaceMessage = "Do not specify parameters for mocked interface.";

        internal const string BadConstructorArgumentsId = "Moq1002";
        internal const string BadConstructorArgumentsTitle = "Moq: No constructors with such parameters";
        internal const string BadConstructorArgumentsMessage = "Parameters provided into mock do not match existing constructors.";

        internal const string BadCallbackSignatureId = "Moq1100";
        internal const string BadCallbackSignatureTitle = "Moq: Bad callback signature";
        internal const string BadCallbackSignatureMessage = "Callback must have the same signature as the mocked method.";

        internal const string MethodInPropertySetupId = "Moq1101";
        internal const string MethodInPropertySetupTitle = "Moq: Method is referenced in property setup";
        internal const string MethodInPropertySetupMessage = "Do not use SetupGet/SetupSet for methods.";
    }
}
