using System.Globalization;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test;

public class RefOutCallbackTests
{
    public static IEnumerable<object[]> TestData()
    {
#pragma warning disable ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing
        object[][] data =
        [

            // Valid ref parameter callback
            ["ref", "string", "Submit", "ref string data", "ref string data", false],

            // Valid out parameter callback
            ["out", "int", "Process", "out int result", "out int result", false],

            // Valid in parameter callback
            ["in", "DateTime", "Handle", "in DateTime timestamp", "in DateTime timestamp", false],

            // Invalid ref parameter mismatch - should detect the mismatch
            ["ref", "string", "Submit", "ref string data", "string data", true]
        ];
#pragma warning restore ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing

        return data.WithNewMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldHandleRefOutInParameterCallbacks(
        string referenceAssemblyGroup,
        string parameterModifier,
        string parameterType,
        string methodName,
        string methodParameter,
        string callbackParameter,
        bool shouldHaveDiagnostic)
    {
        string diagnosticAnnotation = shouldHaveDiagnostic ? "{|Moq1100:" : string.Empty;
        string diagnosticClose = shouldHaveDiagnostic ? "|}" : string.Empty;

        string testCode = $$"""
            using System;

            internal interface IFoo
            {
                void {{methodName}}({{methodParameter}});
            }

            internal delegate void {{methodName}}Callback({{callbackParameter}});

            internal class TestClass
            {
                public void TestMethod()
                {
                    var mock = new Mock<IFoo>();
                    
                    mock.Setup(foo => foo.{{methodName}}({{parameterModifier}} It.Ref<{{parameterType}}>.IsAny))
                        .Callback(new {{methodName}}Callback(({{diagnosticAnnotation}}{{callbackParameter}}{{diagnosticClose}}) => 
                        {
                            {{(string.Equals(parameterModifier, "out", StringComparison.Ordinal) ? string.Create(CultureInfo.InvariantCulture, $"{methodParameter.Split(' ')[^1]} = default;") : string.Empty)}}
                        }));
                }
            }
            """;

        await AnalyzerVerifier<CallbackSignatureShouldMatchMockedMethodAnalyzer>.VerifyAnalyzerAsync(
            testCode,
            referenceAssemblyGroup);
    }
}
