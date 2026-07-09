using System.Reflection;
using Microsoft.CodeAnalysis.Editing;

namespace Moq.Analyzers.Test;

/// <summary>
/// Direct unit tests for the internal <c>Moq.CodeFixes.SyntaxGeneratorExtensions</c> argument
/// validation guards. The throw paths are not exercised by the higher-level code-fix verifier
/// tests because the real fixers never violate the contracts, so they are validated here with
/// positive, negative, and boundary cases for every guard in <c>InsertArguments</c> and
/// <c>ReplaceArgument</c>.
/// </summary>
/// <remarks>
/// The helper is internal to the shipping <c>Moq.CodeFixes</c> assembly. This project reaches it
/// via reflection rather than <c>InternalsVisibleTo</c>: that assembly targets netstandard2.0 and
/// embeds Polyfill-generated BCL attribute types, which collide (CS0433) with the net8.0 framework
/// types once the internals are made visible. Reflection is the same access strategy used by
/// <c>VerifyShouldBeUsedOnlyForOverridableMembersAnalyzerTests.InvokeCanMakeMemberVirtual</c>.
/// </remarks>
public class SyntaxGeneratorExtensionsTests
{
    private static readonly Type ExtensionsType = typeof(Moq.CodeFixes.VerifyOverridableMembersFixer)
        .Assembly
        .GetType("Moq.CodeFixes.SyntaxGeneratorExtensions", throwOnError: true)!;

#pragma warning disable ECS0600 // The literals name methods in another assembly resolved via reflection, not local symbols.
    private static readonly MethodInfo InsertArgumentsMethod =
        ExtensionsType.GetMethod("InsertArguments", BindingFlags.Public | BindingFlags.Static)!;

    private static readonly MethodInfo ReplaceArgumentMethod =
        ExtensionsType.GetMethod("ReplaceArgument", BindingFlags.Public | BindingFlags.Static)!;
#pragma warning restore ECS0600

    [Fact]
    public void InsertArguments_IntoInvocation_InsertsArgument()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M(1, 2)");

        SyntaxNode result = InsertArguments(generator, invocation, 1, Argument("3"));

        Assert.Equal(3, ArgumentCount(result));
        Assert.Equal(["1", "3", "2"], ArgumentExpressions(result));
    }

    [Fact]
    public void InsertArguments_IntoObjectCreation_InsertsArgument()
    {
        SyntaxGenerator generator = CreateGenerator();
        BaseObjectCreationExpressionSyntax creation = ParseCreation("new C(1, 2)");

        SyntaxNode result = InsertArguments(generator, creation, 0, Argument("3"));

        Assert.Equal(3, ArgumentCount(result));
        Assert.Equal(["3", "1", "2"], ArgumentExpressions(result));
    }

    [Fact]
    public void InsertArguments_IntoObjectCreationWithoutArgumentList_InsertsArgument()
    {
        SyntaxGenerator generator = CreateGenerator();
        BaseObjectCreationExpressionSyntax creation = ParseCreation("new C { }");
        Assert.Null(creation.ArgumentList);

        SyntaxNode result = InsertArguments(generator, creation, 0, Argument("3"));

        Assert.Equal(1, ArgumentCount(result));
    }

    [Fact]
    public void InsertArguments_IndexEqualsCount_AppendsArgument()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M(1, 2)");

        SyntaxNode result = InsertArguments(generator, invocation, 2, Argument("3"));

        Assert.Equal(3, ArgumentCount(result));
        Assert.Equal(["1", "2", "3"], ArgumentExpressions(result));
    }

    [Fact]
    public void InsertArguments_ItemNotArgumentSyntax_ThrowsArgumentException()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M(1)");

        Assert.Throws<ArgumentException>(() =>
            InsertArguments(generator, invocation, 0, SyntaxFactory.ParseExpression("3")));
    }

    [Fact]
    public void InsertArguments_UnsupportedSyntaxKind_ThrowsArgumentException()
    {
        SyntaxGenerator generator = CreateGenerator();
        SyntaxNode binary = SyntaxFactory.ParseExpression("a + b");

        Assert.Throws<ArgumentException>(() =>
            InsertArguments(generator, binary, 0, Argument("3")));
    }

    [Fact]
    public void InsertArguments_IndexNegative_ThrowsArgumentOutOfRange()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M(1, 2)");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InsertArguments(generator, invocation, -1, Argument("3")));
    }

    [Fact]
    public void InsertArguments_IndexGreaterThanCount_ThrowsArgumentOutOfRange()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M(1, 2)");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InsertArguments(generator, invocation, 3, Argument("3")));
    }

    [Fact]
    public void ReplaceArgument_InInvocation_ReplacesArgument()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M(1, 2)");

        SyntaxNode result = ReplaceArgument(generator, invocation, 0, Argument("3"));

        Assert.Equal(2, ArgumentCount(result));
        Assert.Equal(["3", "2"], ArgumentExpressions(result));
    }

    [Fact]
    public void ReplaceArgument_InObjectCreation_ReplacesArgument()
    {
        SyntaxGenerator generator = CreateGenerator();
        BaseObjectCreationExpressionSyntax creation = ParseCreation("new C(1, 2)");

        SyntaxNode result = ReplaceArgument(generator, creation, 1, Argument("3"));

        Assert.Equal(2, ArgumentCount(result));
        Assert.Equal(["1", "3"], ArgumentExpressions(result));
    }

    [Fact]
    public void ReplaceArgument_ItemNotArgumentSyntax_ThrowsArgumentException()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M(1)");

        Assert.Throws<ArgumentException>(() =>
            ReplaceArgument(generator, invocation, 0, SyntaxFactory.ParseExpression("3")));
    }

    [Fact]
    public void ReplaceArgument_UnsupportedSyntaxKind_ThrowsArgumentException()
    {
        SyntaxGenerator generator = CreateGenerator();
        SyntaxNode binary = SyntaxFactory.ParseExpression("a + b");

        Assert.Throws<ArgumentException>(() =>
            ReplaceArgument(generator, binary, 0, Argument("3")));
    }

    [Fact]
    public void ReplaceArgument_EmptyInvocation_ThrowsArgumentOutOfRange()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M()");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReplaceArgument(generator, invocation, 0, Argument("3")));
    }

    [Fact]
    public void ReplaceArgument_ObjectCreationWithoutArgumentList_ThrowsArgumentOutOfRange()
    {
        SyntaxGenerator generator = CreateGenerator();
        BaseObjectCreationExpressionSyntax creation = ParseCreation("new C { }");
        Assert.Null(creation.ArgumentList);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReplaceArgument(generator, creation, 0, Argument("3")));
    }

    [Fact]
    public void ReplaceArgument_IndexNegative_ThrowsArgumentOutOfRange()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M(1, 2)");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReplaceArgument(generator, invocation, -1, Argument("3")));
    }

    [Fact]
    public void ReplaceArgument_IndexEqualsCount_ThrowsArgumentOutOfRange()
    {
        SyntaxGenerator generator = CreateGenerator();
        InvocationExpressionSyntax invocation = ParseInvocation("M(1, 2)");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReplaceArgument(generator, invocation, 2, Argument("3")));
    }

    private static SyntaxGenerator CreateGenerator()
    {
        AdhocWorkspace workspace = new();
        return SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
    }

    private static InvocationExpressionSyntax ParseInvocation(string text) =>
        (InvocationExpressionSyntax)SyntaxFactory.ParseExpression(text);

    private static BaseObjectCreationExpressionSyntax ParseCreation(string text) =>
        (BaseObjectCreationExpressionSyntax)SyntaxFactory.ParseExpression(text);

    private static ArgumentSyntax Argument(string expression) =>
        SyntaxFactory.Argument(SyntaxFactory.ParseExpression(expression));

    private static SeparatedSyntaxList<ArgumentSyntax> Arguments(SyntaxNode node) => node switch
    {
        InvocationExpressionSyntax invocation => invocation.ArgumentList.Arguments,
        BaseObjectCreationExpressionSyntax creation => creation.ArgumentList?.Arguments ?? default,
        _ => throw new InvalidOperationException("Unexpected node kind."),
    };

    private static int ArgumentCount(SyntaxNode node) => Arguments(node).Count;

    private static string[] ArgumentExpressions(SyntaxNode node)
    {
        SeparatedSyntaxList<ArgumentSyntax> arguments = Arguments(node);
        string[] expressions = new string[arguments.Count];
        for (int i = 0; i < arguments.Count; i++)
        {
            expressions[i] = arguments[i].Expression.ToString().Trim();
        }

        return expressions;
    }

#pragma warning disable ECS0900 // Reflection invocation boxes the int index argument; unavoidable and confined to test code.
    private static SyntaxNode InsertArguments(SyntaxGenerator generator, SyntaxNode syntax, int index, params SyntaxNode[] items) =>
        (SyntaxNode)Invoke(InsertArgumentsMethod, generator, syntax, index, items)!;

    private static SyntaxNode ReplaceArgument(SyntaxGenerator generator, SyntaxNode syntax, int index, SyntaxNode item) =>
        (SyntaxNode)Invoke(ReplaceArgumentMethod, generator, syntax, index, item)!;
#pragma warning restore ECS0900

    private static object? Invoke(MethodInfo method, params object?[] args)
    {
        try
        {
            return method.Invoke(null, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
