#pragma warning disable ECS0200

namespace Moq.Analyzers.Test.Helpers;

/// <summary>
/// Provides common test data and helpers for doppelganger tests.
/// Doppelganger tests ensure analyzers don't flag user's custom Mock{T} classes.
/// </summary>
internal static class DoppelgangerTestHelper
{
    /// <summary>
    /// Base template for doppelganger tests with a custom Mock{T} class.
    /// </summary>
    public const string CustomMockClassTemplate = """
        namespace TestNamespace.CustomMock;

        public enum MockBehavior
        {{
            Default,
            Strict,
            Loose,
        }}

        internal interface IMyService
        {{
            void Do(string s);
            int Calculate(int a, int b);
            string Property {{ get; set; }}
            System.Threading.Tasks.Task DoAsync();
            System.Threading.Tasks.Task<int> CalculateAsync(int a, int b);
        }}

        internal class MySealedService
        {{
            public void Do(string s) {{ }}
            public int Calculate(int a, int b) => 0;
        }}

        public class Mock<T>
            where T : class
        {{
            public Mock() {{ }}
            public Mock(params object[] args) {{ }}
            public Mock(MockBehavior behavior) {{ }}
            public Mock(MockBehavior behavior, params object[] args) {{ }}
            
            public Mock<TInterface> As<TInterface>() where TInterface : class => new Mock<TInterface>();
            public Mock<T> Setup(System.Linq.Expressions.Expression<System.Action<T>> expression) => this;
            public Mock<T> Setup<TResult>(System.Linq.Expressions.Expression<System.Func<T, TResult>> expression) => this;
            public Mock<T> SetupGet<TProperty>(System.Linq.Expressions.Expression<System.Func<T, TProperty>> expression) => this;
            public Mock<T> SetupSet<TProperty>(System.Action<T> setterExpression) => this;
            public Mock<T> SetupSet(System.Action<T> setterExpression) => this;
            public Mock<T> Callback(System.Action callback) => this;
            public Mock<T> Returns<TResult>(TResult value) => this;
            public Mock<T> ReturnsAsync<TResult>(TResult value) => this;
            public Mock<T> ReturnsAsync<TResult>(System.Threading.Tasks.Task<TResult> value) => this;
        }}

        internal class MyUnitTests
        {{
            private void TestCustomMock()
            {{
                {0}
            }}
        }}
        """;

    /// <summary>
    /// Test data for custom Mock{T} constructor scenarios.
    /// </summary>
    /// <returns>An enumerable of test data arrays containing custom Mock constructor code examples.</returns>
    public static IEnumerable<object[]> CustomMockConstructorData()
    {
        return new object[][]
        {
            ["""var mock1 = new Mock<IMyService>();"""],
            ["""var mock2 = new Mock<IMyService>("param");"""],
            ["""var mock3 = new Mock<IMyService>(5, true);"""],
            ["""var mock4 = new Mock<IMyService>(MockBehavior.Strict);"""],
            ["""var mock5 = new Mock<IMyService>(MockBehavior.Strict, 6, true);"""],
            ["""var mock6 = new Mock<MySealedService>();"""],
        };
    }

    /// <summary>
    /// Test data for custom Mock{T} method call scenarios.
    /// </summary>
    /// <returns>An enumerable of test data arrays containing custom Mock method call code examples.</returns>
    public static IEnumerable<object[]> CustomMockMethodCallData()
    {
        return new object[][]
        {
            // As() method calls
            ["""var mock = new Mock<IMyService>().As<IMyService>();"""],

            // Setup method calls
            ["""var mock = new Mock<IMyService>().Setup(x => x.Do("test"));"""],
            ["""var mock = new Mock<IMyService>().Setup(x => x.Calculate(1, 2));"""],
            ["""new Mock<IMyService>().Setup(x => x.Do("test"));"""],

            // SetupGet/SetupSet calls
            ["""var mock = new Mock<IMyService>().SetupGet(x => x.Property);"""],
            ["""var mock = new Mock<IMyService>().SetupSet(x => x.Property = "value");"""],

            // Returns/Callback chaining
            ["""var mock = new Mock<IMyService>().Setup(x => x.Calculate(1, 2)).Returns(42);"""],
            ["""var mock = new Mock<IMyService>().Setup(x => x.Calculate(1, 2)).Callback(() => { });"""],
            ["""new Mock<IMyService>().Setup(x => x.Calculate(1, 2)).ReturnsAsync(42);"""],

            // Complex chaining
            ["""var mock = new Mock<IMyService>().As<IMyService>().Setup(x => x.Calculate(1, 2)).Returns(42);"""],
        };
    }

    /// <summary>
    /// Gets combined test data for all custom Mock{T} scenarios.
    /// </summary>
    /// <returns>An enumerable of test data arrays containing all custom Mock constructor and method call code examples.</returns>
    public static IEnumerable<object[]> GetAllCustomMockData()
    {
        return CustomMockConstructorData().Concat(CustomMockMethodCallData());
    }

    /// <summary>
    /// Creates a test string from the template with the provided mock code.
    /// </summary>
    /// <param name="mockCode">The mock code to insert into the test template.</param>
    /// <returns>A formatted test code string with the mock code inserted into the template.</returns>
    public static string CreateTestCode(string mockCode)
    {
        return string.Format(CustomMockClassTemplate, mockCode);
    }
}
