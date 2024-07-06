using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.Test.CompositeAnalyzer>;

namespace Moq.Analyzers.Test;

public class MoqQuickstartTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Returns
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.DoSomething("ping")).Returns(true);
             """],

            // Out arguments
            ["""
             var mock = new Mock<IFoo>();
             var outString = "ack";
             // TryParse will return true, and the out argument will return "ack", lazy evaluated
             mock.Setup(foo => foo.TryParse("ping", out outString)).Returns(true);
             """],

            // ref arguments
            ["""
             var mock = new Mock<IFoo>();
             var instance = new Bar();
             
             // Only matches if the ref argument to the invocation is the same instance
             mock.Setup(foo => foo.Submit(ref instance)).Returns(true);
             """],

            // access invocation arguments when returning a value
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.DoSomethingStringy(It.IsAny<string>())).Returns((string s) => s.ToLower());
            """],

            // Multiple parameters overloads available
            // throwing when invoked with specific parameters
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(foo => foo.DoSomething("reset")).Throws<InvalidOperationException>();
            mock.Setup(foo => foo.DoSomething("")).Throws(new ArgumentException("command"));
            """],

            // lazy evaluating return value
            ["""
            var mock = new Mock<IFoo>();
            var count = 1;
            mock.Setup(foo => foo.GetCount()).Returns(() => count);
            """],

            // ### ASYNC METHODS ###
            // There are several ways to setup "async" methods (e.g., returning a Task<T> or ValueTask<T>)
            // Prior to 4.16 this was not permitted and Moq1201 would be correct
            // in suggesting to use setup helper methods like .ReturnsAsync and .ThrowsAsync
            // when they are available.
            // Starting with 4.16 you can simply `mock.Setup` the returned tasks's `.Result` property
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.DoSomethingAsync().Result).Returns(true);
             """],

            // In versions prior to 4.16, use setup helper methods
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.DoSomethingAsync()).ReturnsAsync(true);
             """],

            // This is also allowed, but will trigger a compiler warning
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.DoSomethingAsync()).Returns(async () => 42);
             """],

            // ### Matching Arguments ###
            //
            // any value
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.DoSomething(It.IsAny<string>())).Returns(true);
             """],

            // any value passed in a `ref` parameter (requires Moq 4.8 or later)
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.Submit(ref It.Ref<Bar>.IsAny)).Returns(true);
             """],

            // matching Func<int>, lazy evaluated
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.Add(It.Is<int>(i => i % 2 == 0))).Returns(true); 
             """],

            // matching ranges
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.Add(It.IsInRange<int>(0, 10, Moq.Range.Inclusive))).Returns(true); 
             """],

            // matching regex
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(x => x.DoSomethingStringy(It.IsRegex("[a-d]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase))).Returns("foo");
             """],

            // ### Properties ###
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.Name).Returns("bar");
             """],

            // auto-mocking hierarchies (a.k.a. recursive mocks)
            ["""
             var mock = new Mock<IFoo>();
             mock.Setup(foo => foo.Bar.Baz.Name).Returns("baz");
             """],

            // expects an invocation to set the value to "foo"
            ["""
             var mock = new Mock<IFoo>();
             mock.SetupSet(foo => foo.Name = "foo");
             """],

            // or verify the setter directly
            ["""
             var mock = new Mock<IFoo>();
             mock.VerifySet(foo => foo.Name = "foo");
             """],

            // Setup a property so that it will automatically track its value (a Stub)
            ["""
             var mock = new Mock<IFoo>();
             mock.SetupProperty(f => f.Name);
             """],

            // Provide a default value for the stubbed property
            ["""
             var mock = new Mock<IFoo>();
             mock.SetupProperty(f => f.Name, "foo");
             """],

            // A more complex example
            ["""
             var mock = new Mock<IFoo>();
             mock.SetupProperty(f => f.Name, "foo");
             
             IFoo foo = mock.Object;
             // Initial value was stored
             
             // New value set which changes the initial value
             foo.Name = "bar";
             """],

            // Stub all properties on a mock
            ["""
             var mock = new Mock<IFoo>();
             mock.SetupAllProperties();
             """],

            // ### Events ###
            // TODO
        }.WithReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task MethodsExample(string referenceAssemblyGroup, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using Moq;
            
            // Assumptions:
            public interface IFoo
            {
                Bar Bar { get; set; }
                string Name { get; set; }
                int Value { get; set; }
                bool DoSomething(string value);
                bool DoSomething(int number, string value);
                Task<bool> DoSomethingAsync();
                string DoSomethingStringy(string value);
                bool TryParse(string value, out string outputValue);
                bool Submit(ref Bar bar);
                int GetCount();
                bool Add(int value);
            }
            
            public class Bar 
            {
                public virtual Baz Baz { get; set; }
                public virtual bool Submit() { return false; }
            }
            
            public class Baz
            {
                public virtual string Name { get; set; }
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    {{mock}}
                }
            }
            """,
            referenceAssemblyGroup);
    }
}
