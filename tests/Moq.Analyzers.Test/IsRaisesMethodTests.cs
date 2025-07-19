using Verifier = Moq.Analyzers.Test.Helpers.AllAnalyzersVerifier;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests for the symbol-based Raises method detection functionality.
/// These tests verify that IsRaisesMethodCall and IsRaisesInvocation correctly identify
/// valid and invalid Raises patterns using symbol-based detection.
/// </summary>
public class IsRaisesMethodTests
{
    public static IEnumerable<object[]> ValidRaisesPatterns()
    {
        return new object[][]
        {
            // Basic Raises call
            ["""mock.Setup(x => x.DoSomething()).Raises(x => x.MyEvent += null, "arg");"""],

            // RaisesAsync call
            ["""mock.Setup(x => x.DoSomethingAsync()).RaisesAsync(x => x.MyAsyncEvent += null, "arg");"""],

            // Multiple method chains
            ["""mock.Setup(x => x.Method()).Returns("value").Raises(x => x.Event += null, 42);"""],

            // Different event types
            ["""mock.Setup(x => x.Process()).Raises(x => x.IntEvent += null, 123);"""],
            ["""mock.Setup(x => x.Process()).Raises(x => x.ActionEvent += null);"""],

            // With different mock setups
            ["""mockService.Setup(service => service.ExecuteCommand()).Raises(s => s.CommandExecuted += null, "command");"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidRaisesPatterns()
    {
        return new object[][]
        {
            // Not a Raises method - should not be detected as Raises call
            ["""mock.Setup(x => x.DoSomething()).Returns("value");"""],

            // Similar named methods but not actual Raises
            ["""someObject.MyRaises("not a moq call");"""],
            ["""mock.Setup(x => x.DoSomething()).Callback(() => { });"""],

            // Method without Setup chain
            ["""mock.Raises(x => x.Event += null, "arg");"""], // Direct Raises without Setup

            // Non-Moq method calls
            ["""console.WriteLine("test");"""],
            ["""string.Join(",", items);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(ValidRaisesPatterns))]
    public async Task ShouldDetectValidRaisesPatterns(string referenceAssemblyGroup, string @namespace, string raisesCall)
    {
        // Test that valid Raises patterns don't trigger unwanted diagnostics
        static string Template(string ns, string call) =>
$$"""
{{ns}}

public interface ITestService
{
    event Action<string> MyEvent;
    event Action<string> MyAsyncEvent;
    event Action<int> IntEvent;
    event Action ActionEvent;
    event Action<string> CommandExecuted;
    
    void DoSomething();
    Task DoSomethingAsync();
    void Method();
    void Process();
    void ExecuteCommand();
}

public class TestClass
{
    public void TestMethod()
    {
        var mock = new Mock<ITestService>(MockBehavior.Strict);
        var mockService = new Mock<ITestService>(MockBehavior.Strict);
        {{call}}
    }
}
""";

        string source = Template(@namespace, raisesCall);
        await AllAnalyzersVerifier.VerifyAllAnalyzersAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(InvalidRaisesPatterns))]
    public async Task ShouldNotDetectInvalidRaisesPatterns(string referenceAssemblyGroup, string @namespace, string nonRaisesCall)
    {
        // Test that non-Raises patterns don't get false positives
        static string Template(string ns, string call) =>
$$"""
{{ns}}

public interface ITestService
{
    event Action<string> Event;
    void DoSomething();
}

public class MyObject
{
    public void MyRaises(string arg) { }
}

public class TestClass
{
    public void TestMethod()
    {
        var mock = new Mock<ITestService>(MockBehavior.Strict);
        var someObject = new MyObject();
        var console = Console.Out;
        var items = new[] { "a", "b" };
        
        {{call}}
    }
}
""";

        string source = Template(@namespace, nonRaisesCall);
        await AllAnalyzersVerifier.VerifyAllAnalyzersAsync(source, referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldHandleComplexRaisesChains()
    {
        const string source = """
namespace Test;

public interface IComplexService
{
    event Action<string, int> ComplexEvent;
    event EventHandler<EventArgs> StandardEvent;
    void ProcessData();
    Task<string> ProcessAsync();
}

public class TestComplexRaises
{
    public void TestMethod()
    {
        var mock = new Mock<IComplexService>(MockBehavior.Strict);
        
        // Complex chaining with multiple calls
        mock.Setup(x => x.ProcessData())
            .Raises(x => x.ComplexEvent += null, "data", 42);
            
        // Standard event handler pattern
        mock.Setup(x => x.ProcessAsync())
            .ReturnsAsync("result")
            .Raises(x => x.StandardEvent += null, EventArgs.Empty);
    }
}
""";

        await AllAnalyzersVerifier.VerifyAllAnalyzersAsync(source, "Net80WithNewMoq");
    }
}
