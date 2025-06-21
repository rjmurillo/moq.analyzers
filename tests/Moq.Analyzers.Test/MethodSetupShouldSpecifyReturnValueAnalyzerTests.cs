using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.MethodSetupShouldSpecifyReturnValueAnalyzer>;

namespace Moq.Analyzers.Test;

public class MethodSetupShouldSpecifyReturnValueAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        // Test cases where a method setup should specify return value but doesn't
        IEnumerable<object[]> both = new object[][]
        {
            // Method with return type should specify return value
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.DoSomething("test"))|};"""],
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.GetValue())|};"""],
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.Calculate(1, 2))|};"""],

            // Valid cases - method with return type that does specify return value
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Returns(true);"""],
            ["""new Mock<IFoo>().Setup(x => x.GetValue()).Returns(42);"""],
            ["""new Mock<IFoo>().Setup(x => x.Calculate(1, 2)).Returns(10);"""],
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Throws<InvalidOperationException>();"""],
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Throws(new ArgumentException());"""],

            // Void methods should not trigger the analyzer
            ["""new Mock<IFoo>().Setup(x => x.DoVoidMethod());"""],
            ["""new Mock<IFoo>().Setup(x => x.ProcessData("test"));"""],

            // Property setups should not trigger this analyzer
            ["""new Mock<IFoo>().Setup(x => x.Name);"""],
            ["""new Mock<IFoo>().SetupGet(x => x.Name);"""],

            // Edge cases to ensure all return guards are covered
            // Non-setup methods should not trigger analyzer
            ["""new Mock<IFoo>().SetupProperty(x => x.Name);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return both;
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMethodSetupReturnValue(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source = $$"""
            {{@namespace}}

            public interface IFoo
            {
                bool DoSomething(string value);
                int GetValue();
                int Calculate(int a, int b);
                void DoVoidMethod();
                void ProcessData(string data);
                string Name { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{mock}}
                }
            }
            """;

        output.WriteLine(source);

        await Verifier.VerifyAnalyzerAsync(
            source,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Theory]
    [InlineData("object")]
    [InlineData("dynamic")]
    [InlineData("string")]
    public async Task ShouldNotCrashOnMethodsWithComplexReturnTypes(string returnType)
    {
        string source = $$"""
            using Moq;
            using System;

            public interface IFoo
            {
                {{returnType}} GetValue();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {|Moq1203:new Mock<IFoo>().Setup(x => x.GetValue())|};
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldNotTriggerOnMethodsWithoutLambdaExpression()
    {
        string source = """
            using Moq;
            using System;

            public interface IFoo
            {
                string GetValue();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    // Non-lambda setups should not crash the analyzer
                    var mock = new Mock<IFoo>();
                    mock.CallBase = true;
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldHandleGenericMethods()
    {
        string source = """
            using Moq;
            using System;

            public interface IFoo
            {
                T GetValue<T>();
                void SetValue<T>(T value);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IFoo>();
                    {|Moq1203:mock.Setup(x => x.GetValue<string>())|};
                    mock.Setup(x => x.SetValue<int>(42));
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldNotTriggerWhenChainedWithOtherMethods()
    {
        string source = """
            using Moq;
            using System;

            public interface IFoo
            {
                string GetValue();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IFoo>();
                    // Chained with non-Returns/Throws methods should still trigger
                    {|Moq1203:mock.Setup(x => x.GetValue())|}.Callback(() => { });
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldHandleAsyncMethods()
    {
        string source = """
            using Moq;
            using System;
            using System.Threading.Tasks;

            public interface IFoo
            {
                Task<string> GetValueAsync();
                Task DoWorkAsync();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IFoo>();
                    {|Moq1203:mock.Setup(x => x.GetValueAsync())|};
                    {|Moq1203:mock.Setup(x => x.DoWorkAsync())|};
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
