using Moq.Analyzers;

namespace Moq.Analyzers.CSharp14.Test;

public class CSharp14CrashSafetyTests
{
    private static readonly string FieldBackedPropertySource =
        """
        public class FieldBackedPropertyService
        {
            public virtual int Value
            {
                get => field;
                set => field = value;
            }
        }

        public class UnitTest
        {
            private void Test()
            {
                var mock = new Mock<FieldBackedPropertyService>(MockBehavior.Strict);
                mock.Setup(x => x.Value).Returns(42);
            }
        }
        """;

    private static readonly string NullConditionalAssignmentSource =
        """
        public interface IWriter
        {
            void Write(int value);
        }

        public class Receiver
        {
            public int Value { get; set; }
        }

        public class UnitTest
        {
            private void Test()
            {
                Receiver? receiver = new();
                var mock = new Mock<IWriter>(MockBehavior.Strict);

                mock.Setup(x => x.Write(It.IsAny<int>()))
                    .Callback<int>(value => { receiver?.Value = value; });
            }
        }
        """;

    private static readonly string ExtensionBlockSource =
        """
        public class ExtensionBlockService
        {
            public virtual int GetValue() => 1;
        }

        public static class ExtensionBlockServiceExtensions
        {
            extension(ExtensionBlockService service)
            {
                public int DoubledValue => service.GetValue() * 2;
            }
        }

        public class UnitTest
        {
            private void Test()
            {
                var mock = new Mock<ExtensionBlockService>(MockBehavior.Strict);
                mock.Setup(x => x.GetValue()).Returns(21);

                int value = mock.Object.DoubledValue;
            }
        }
        """;

    public static IEnumerable<object[]> ParamsCollectionConstructorData()
    {
        return new object[][]
        {
            ["IEnumerable<int>", """new Mock<ClassWithParamsCollection>();"""],
            ["IEnumerable<int>", """new Mock<ClassWithParamsCollection>(1, 2, 3);"""],
            ["IEnumerable<int>", """new Mock<ClassWithParamsCollection>(MockBehavior.Default, 1, 2, 3);"""],
            ["IEnumerable<int>", """new Mock<ClassWithParamsCollection>{|Moq1002:("not an int")|};"""],
            ["ReadOnlySpan<int>", """new Mock<ClassWithParamsCollection>();"""],
            ["ReadOnlySpan<int>", """new Mock<ClassWithParamsCollection>(1, 2, 3);"""],
            ["ReadOnlySpan<int>", """new Mock<ClassWithParamsCollection>(MockBehavior.Default, 1, 2, 3);"""],
            ["ReadOnlySpan<int>", """new Mock<ClassWithParamsCollection>{|Moq1002:("not an int")|};"""],
        };
    }

    public static IEnumerable<object[]> ValidParamsCollectionConstructorData()
    {
        return new object[][]
        {
            ["IEnumerable<int>", """new Mock<ClassWithParamsCollection>(MockBehavior.Strict);"""],
            ["IEnumerable<int>", """new Mock<ClassWithParamsCollection>(MockBehavior.Strict, 1, 2, 3);"""],
            ["ReadOnlySpan<int>", """new Mock<ClassWithParamsCollection>(MockBehavior.Strict);"""],
            ["ReadOnlySpan<int>", """new Mock<ClassWithParamsCollection>(MockBehavior.Strict, 1, 2, 3);"""],
        };
    }

    [Theory]
    [MemberData(nameof(ParamsCollectionConstructorData))]
    public async Task ShouldAnalyzeCSharp14ParamsCollectionConstructors(string paramsType, string mock)
    {
        await CSharp14AnalyzerVerifier<ConstructorArgumentsShouldMatchAnalyzer>.VerifyAnalyzerAsync(
            $$"""
              public class ClassWithParamsCollection
              {
                  public ClassWithParamsCollection(params {{paramsType}} nums) { }
              }

              public class UnitTest
              {
                  private void Test()
                  {
                      {{mock}}
                  }
              }
              """);
    }

    [Theory]
    [MemberData(nameof(ValidParamsCollectionConstructorData))]
    public async Task ShouldNotCrashAnyAnalyzerOnCSharp14ParamsCollectionConstructors(string paramsType, string mock)
    {
        await CSharp14AllAnalyzersVerifier.VerifyAllAnalyzersAsync(
            $$"""
              public class ClassWithParamsCollection
              {
                  public ClassWithParamsCollection(params {{paramsType}} nums) { }
              }

              public class UnitTest
              {
                  private void Test()
                  {
                      {{mock}}
                  }
              }
              """);
    }

    [Fact]
    public async Task ShouldNotCrashAnyAnalyzerOnFieldBackedProperties()
    {
        await CSharp14AllAnalyzersVerifier.VerifyAllAnalyzersAsync(FieldBackedPropertySource);
    }

    [Fact]
    public async Task ShouldNotCrashAnyAnalyzerOnNullConditionalAssignment()
    {
        await CSharp14AllAnalyzersVerifier.VerifyAllAnalyzersAsync(NullConditionalAssignmentSource);
    }

    [Fact]
    public async Task ShouldNotCrashAnyAnalyzerOnExtensionBlocks()
    {
        await CSharp14AllAnalyzersVerifier.VerifyAllAnalyzersAsync(ExtensionBlockSource);
    }
}
