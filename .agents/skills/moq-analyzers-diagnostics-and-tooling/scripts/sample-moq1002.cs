// Known-positive sample for run-analyzer-on-snippet.sh.
// Expected: Moq1002 (constructor arguments do not match any constructor of the mocked class)
// on the argument list `(1, true)` — Foo has only a (string) constructor.
// Also expected: Moq1400 and Moq1410 (explicit/strict MockBehavior) on `new Mock<Foo>`,
// because no MockBehavior argument is passed and the harness .editorconfig raises
// their default Info severity to warning so they appear in build output.
using Moq;

namespace SnippetSample;

public class Foo
{
    public Foo(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public virtual int Bar()
    {
        return 42;
    }
}

public static class Repro
{
    public static void Run()
    {
        var mock = new Mock<Foo>(1, true); // Moq1002: (1, true) matches no Foo constructor
        _ = mock.Object;
    }
}
