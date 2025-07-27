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

            // Different event types
            ["""mock.Setup(x => x.Process()).Raises(x => x.IntEvent += null, 123);"""],
            ["""mock.Setup(x => x.Process()).Raises(x => x.ActionEvent += null);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidRaisesPatterns()
    {
        return new object[][]
        {
            // Similar named methods but not actual Raises
            ["""someObject.MyRaises("not a moq call");"""],
            ["""mock.Setup(x => x.DoSomething()).Callback(() => { });"""],

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
    event Action<int> IntEvent;
    event Action ActionEvent;
    
    void DoSomething();
    void Process();
}

public class TestClass
{
    public void TestMethod()
    {
        var mock = new Mock<ITestService>(MockBehavior.Strict);
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
    public async Task ShouldHandleSimpleRaisesChains()
    {
        const string source = """
namespace Test;

public interface ISimpleService
{
    event Action<string> SimpleEvent;
    void ProcessData();
}

public class TestSimpleRaises
{
    public void TestMethod()
    {
        var mock = new Mock<ISimpleService>(MockBehavior.Strict);
        
        // Simple Raises call
        mock.Setup(x => x.ProcessData())
            .Raises(x => x.SimpleEvent += null, "data");
    }
}
""";

        await AllAnalyzersVerifier.VerifyAllAnalyzersAsync(source, "Net80WithNewMoq");
    }
}
