namespace Moq.Analyzers.Test.Common;

public class IMethodSymbolExtensionsTests
{
    [Fact]
    public void Overloads_MethodWithOverloads_ReturnsOnlyOtherOverloads()
    {
        const string code = @"
class C
{
    void M(int x) { }
    void M(string x) { }
    void M(int x, string y) { }
}";
        (IMethodSymbol targetMethod, _) = GetMethodAndOverloads(code, "M", 0);

        IMethodSymbol[] result = targetMethod.Overloads().ToArray();

        Assert.Equal(2, result.Length);
        Assert.DoesNotContain(targetMethod, result, SymbolEqualityComparer.Default);
        Assert.All(result, m => Assert.Equal("M", m.Name));
    }

    [Fact]
    public void Overloads_MethodWithNoOverloads_ReturnsEmpty()
    {
        const string code = @"
class C
{
    void M(int x) { }
    void N(string x) { }
}";
        (IMethodSymbol targetMethod, _) = GetMethodAndOverloads(code, "M", 0);

        IMethodSymbol[] result = targetMethod.Overloads().ToArray();

        Assert.Empty(result);
    }

    [Fact]
    public void Overloads_NullMethod_ReturnsEmpty()
    {
        IMethodSymbol? nullMethod = null;

        IMethodSymbol[] result = nullMethod.Overloads().ToArray();

        Assert.Empty(result);
    }

    [Fact]
    public void Overloads_CustomComparer_UsesProvidedComparer()
    {
        const string code = @"
class C
{
    void M(int x) { }
    void M(string x) { }
    void M(int x, string y) { }
}";
        (IMethodSymbol targetMethod, _) = GetMethodAndOverloads(code, "M", 0);

        // SymbolEqualityComparer.IncludeNullability behaves the same as Default for non-nullable types,
        // but this verifies the comparer parameter is threaded through.
        IMethodSymbol[] result = targetMethod.Overloads(SymbolEqualityComparer.IncludeNullability).ToArray();

        Assert.Equal(2, result.Length);
        Assert.DoesNotContain(targetMethod, result, SymbolEqualityComparer.Default);
    }

    [Fact]
    public void TryGetOverloadWithParameterOfType_OverloadHasMatchingParameter_ReturnsTrueWithMatches()
    {
        const string code = @"
class C
{
    void M(int x) { }
    void M(string x) { }
    void M(int x, string y) { }
}";
        (SemanticModel model, IMethodSymbol intMethod, IReadOnlyList<IMethodSymbol> allMethods) =
            GetMethodContextWithAllOverloads(code, "M", 0);
        INamedTypeSymbol stringType = model.Compilation.GetSpecialType(SpecialType.System_String);

        bool found = intMethod.TryGetOverloadWithParameterOfType(
            allMethods,
            stringType,
            out IMethodSymbol? methodMatch,
            out IParameterSymbol? parameterMatch);

        Assert.True(found);
        Assert.NotNull(methodMatch);
        Assert.NotNull(parameterMatch);
        Assert.True(SymbolEqualityComparer.Default.Equals(parameterMatch!.Type, stringType));
    }

    [Fact]
    public void TryGetOverloadWithParameterOfType_NoOverloadHasMatchingParameter_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M(int x) { }
    void M(string x) { }
}";
        (SemanticModel model, IMethodSymbol intMethod, IReadOnlyList<IMethodSymbol> allMethods) =
            GetMethodContextWithAllOverloads(code, "M", 0);
        INamedTypeSymbol doubleType = model.Compilation.GetSpecialType(SpecialType.System_Double);

        bool found = intMethod.TryGetOverloadWithParameterOfType(
            allMethods,
            doubleType,
            out IMethodSymbol? methodMatch,
            out IParameterSymbol? parameterMatch);

        Assert.False(found);
        Assert.Null(methodMatch);
        Assert.Null(parameterMatch);
    }

    [Fact]
    public void TryGetOverloadWithParameterOfType_CurrentMethodInOverloadsList_SkipsSelf()
    {
        const string code = @"
class C
{
    void M(int x) { }
}";
        (SemanticModel model, IMethodSymbol intMethod, _) =
            GetMethodContextWithAllOverloads(code, "M", 0);
        INamedTypeSymbol intType = model.Compilation.GetSpecialType(SpecialType.System_Int32);

        // Pass the method itself in the overloads list. It should be skipped via comparer.Equals.
        bool found = intMethod.TryGetOverloadWithParameterOfType(
            new[] { intMethod },
            intType,
            out IMethodSymbol? methodMatch,
            out IParameterSymbol? parameterMatch);

        Assert.False(found);
        Assert.Null(methodMatch);
        Assert.Null(parameterMatch);
    }

    [Fact]
    public void TryGetOverloadWithParameterOfType_CancellationRequested_ThrowsOperationCanceledException()
    {
        const string code = @"
class C
{
    void M(int x) { }
    void M(string x) { }
}";
        (SemanticModel model, IMethodSymbol intMethod, IReadOnlyList<IMethodSymbol> allMethods) =
            GetMethodContextWithAllOverloads(code, "M", 0);
        INamedTypeSymbol stringType = model.Compilation.GetSpecialType(SpecialType.System_String);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
        {
            intMethod.TryGetOverloadWithParameterOfType(
                allMethods,
                stringType,
                out _,
                out _,
                cancellationToken: cts.Token);
        });
    }

    [Fact]
    public void TryGetOverloadWithParameterOfType_ConvenienceOverload_DelegatesToFullVersion()
    {
        const string code = @"
class C
{
    void M(int x) { }
    void M(string x) { }
    void M(int x, string y) { }
}";
        (SemanticModel model, IMethodSymbol intMethod, _) =
            GetMethodContextWithAllOverloads(code, "M", 0);
        INamedTypeSymbol stringType = model.Compilation.GetSpecialType(SpecialType.System_String);

        bool found = intMethod.TryGetOverloadWithParameterOfType(
            stringType,
            out IMethodSymbol? methodMatch,
            out IParameterSymbol? parameterMatch);

        Assert.True(found);
        Assert.NotNull(methodMatch);
        Assert.NotNull(parameterMatch);
        Assert.True(SymbolEqualityComparer.Default.Equals(parameterMatch!.Type, stringType));
    }

    [Fact]
    public void TryGetParameterOfType_MethodHasParameterOfType_ReturnsTrueWithMatch()
    {
        const string code = @"
class C
{
    void M(int x, string y) { }
}";
        (SemanticModel model, IMethodSymbol method, _) =
            GetMethodContextWithAllOverloads(code, "M", 0);
        INamedTypeSymbol stringType = model.Compilation.GetSpecialType(SpecialType.System_String);

        bool found = method.TryGetParameterOfType(stringType, out IParameterSymbol? match);

        Assert.True(found);
        Assert.NotNull(match);
        Assert.Equal("y", match!.Name);
        Assert.True(SymbolEqualityComparer.Default.Equals(match.Type, stringType));
    }

    [Fact]
    public void TryGetParameterOfType_MethodHasNoParameterOfType_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M(int x) { }
}";
        (SemanticModel model, IMethodSymbol method, _) =
            GetMethodContextWithAllOverloads(code, "M", 0);
        INamedTypeSymbol doubleType = model.Compilation.GetSpecialType(SpecialType.System_Double);

        bool found = method.TryGetParameterOfType(doubleType, out IParameterSymbol? match);

        Assert.False(found);
        Assert.Null(match);
    }

    [Fact]
    public void TryGetParameterOfType_CancellationRequested_ThrowsOperationCanceledException()
    {
        const string code = @"
class C
{
    void M(int x, string y) { }
}";
        (SemanticModel model, IMethodSymbol method, _) =
            GetMethodContextWithAllOverloads(code, "M", 0);
        INamedTypeSymbol stringType = model.Compilation.GetSpecialType(SpecialType.System_String);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
        {
            method.TryGetParameterOfType(stringType, out _, cancellationToken: cts.Token);
        });
    }

    private static (IMethodSymbol TargetMethod, IReadOnlyList<IMethodSymbol> AllOverloads) GetMethodAndOverloads(
        string code,
        string methodName,
        int index)
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        MethodDeclarationSyntax[] methods = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => string.Equals(m.Identifier.Text, methodName, StringComparison.Ordinal))
            .ToArray();

        IMethodSymbol target = model.GetDeclaredSymbol(methods[index])!;
        IMethodSymbol[] allOverloads = methods
            .Select(m => model.GetDeclaredSymbol(m)!)
            .ToArray();

        return (target, allOverloads);
    }

    private static (SemanticModel Model, IMethodSymbol TargetMethod, IReadOnlyList<IMethodSymbol> AllMethods) GetMethodContextWithAllOverloads(
        string code,
        string methodName,
        int index)
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        MethodDeclarationSyntax[] methods = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => string.Equals(m.Identifier.Text, methodName, StringComparison.Ordinal))
            .ToArray();

        IMethodSymbol target = model.GetDeclaredSymbol(methods[index])!;
        IMethodSymbol[] allMethods = methods
            .Select(m => model.GetDeclaredSymbol(m)!)
            .ToArray();

        return (model, target, allMethods);
    }
}
