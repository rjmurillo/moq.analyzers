using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers.Test.Common;

public class IOperationExtensionsTests
{
    private static readonly MetadataReference CorlibReference;
    private static readonly MetadataReference SystemRuntimeReference;

#pragma warning disable S3963 // "static fields" should be initialized inline - conflicts with ECS1300
    static IOperationExtensionsTests()
    {
        CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        SystemRuntimeReference = MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll"));
    }
#pragma warning restore S3963

    private static MetadataReference[] CoreReferences => [CorlibReference, SystemRuntimeReference];

    [Fact]
    public void WalkDownConversion_NonConversionOperation_ReturnsSelf()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
    }
}";
        IOperation operation = GetFirstOperationOfType<IVariableDeclaratorOperation>(code);

        IOperation result = operation.WalkDownConversion();

        Assert.Same(operation, result);
    }

    [Fact]
    public void WalkDownConversion_SingleConversion_UnwrapsToOperand()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
        long y = x;
    }
}";
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
        VariableDeclaratorSyntax declarator = tree.GetRoot()
            .DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .First(v => string.Equals(v.Identifier.Text, "y", StringComparison.Ordinal));
        IOperation? declOp = model.GetOperation(declarator);
        Assert.NotNull(declOp);
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        IConversionOperation conversion = declOp!.ChildOperations
            .SelectMany(Flatten)
            .OfType<IConversionOperation>()
            .First();
#pragma warning restore ECS0900

        IOperation result = conversion.WalkDownConversion();

        Assert.IsNotAssignableFrom<IConversionOperation>(result);
    }

    [Fact]
    public void WalkDownConversion_NestedConversions_UnwrapsToInnermost()
    {
        const string code = @"
class C
{
    void M()
    {
        byte b = 1;
        long y = b;
    }
}";
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
        VariableDeclaratorSyntax declarator = tree.GetRoot()
            .DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .First(v => string.Equals(v.Identifier.Text, "y", StringComparison.Ordinal));
        IOperation? initializerOp = model.GetOperation(declarator.Initializer!.Value);
        Assert.NotNull(initializerOp);

        IOperation result = initializerOp!.WalkDownConversion();

        Assert.IsNotAssignableFrom<IConversionOperation>(result);
    }

    [Fact]
    public void WalkDownImplicitConversion_NonConversionOperation_ReturnsSelf()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
    }
}";
        IOperation operation = GetFirstOperationOfType<IVariableDeclaratorOperation>(code);

        IOperation result = operation.WalkDownImplicitConversion();

        Assert.Same(operation, result);
    }

    [Fact]
    public void WalkDownImplicitConversion_ImplicitConversion_UnwrapsToOperand()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
        long y = x;
    }
}";
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
        VariableDeclaratorSyntax declarator = tree.GetRoot()
            .DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .First(v => string.Equals(v.Identifier.Text, "y", StringComparison.Ordinal));
        IOperation? declOp = model.GetOperation(declarator);
        Assert.NotNull(declOp);
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        IConversionOperation conversion = declOp!.ChildOperations
            .SelectMany(Flatten)
            .OfType<IConversionOperation>()
            .First();
#pragma warning restore ECS0900

        IOperation result = conversion.WalkDownImplicitConversion();

        Assert.IsNotAssignableFrom<IConversionOperation>(result);
    }

    [Fact]
    public void WalkDownImplicitConversion_ExplicitConversion_StopsAtExplicit()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
        short y = (short)x;
    }
}";
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
        CastExpressionSyntax castExpr = tree.GetRoot()
            .DescendantNodes().OfType<CastExpressionSyntax>().First();
        IOperation? castOp = model.GetOperation(castExpr);
        Assert.NotNull(castOp);
        Assert.IsAssignableFrom<IConversionOperation>(castOp);

        IOperation result = castOp!.WalkDownImplicitConversion();

        Assert.Same(castOp, result);
    }

    [Fact]
    public void WalkDownImplicitConversion_NestedImplicitConversions_UnwrapsAll()
    {
        const string code = @"
class C
{
    void M()
    {
        byte b = 1;
        long y = b;
    }
}";
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
        VariableDeclaratorSyntax declarator = tree.GetRoot()
            .DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .First(v => string.Equals(v.Identifier.Text, "y", StringComparison.Ordinal));
        IOperation? initializerOp = model.GetOperation(declarator.Initializer!.Value);
        Assert.NotNull(initializerOp);

        IOperation result = initializerOp!.WalkDownImplicitConversion();

        Assert.IsNotAssignableFrom<IConversionOperation>(result);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_NullOperation_ReturnsNull()
    {
        IOperation? nullOperation = null;

        ISymbol? result = nullOperation.GetReferencedMemberSymbolFromLambda();

        Assert.Null(result);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_ExpressionLambdaWithPropertyReference_ReturnsPropertySymbol()
    {
        const string code = @"
class C
{
    int Prop { get; set; }
    void M()
    {
        System.Func<C, int> f = c => c.Prop;
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IPropertySymbol>(result);
        Assert.Equal("Prop", result!.Name);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_ExpressionLambdaWithMethodInvocation_ReturnsMethodSymbol()
    {
        const string code = @"
class C
{
    int GetValue() => 42;
    void M()
    {
        System.Func<C, int> f = c => c.GetValue();
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IMethodSymbol>(result);
        Assert.Equal("GetValue", result!.Name);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_BlockLambdaWithPropertyReturn_ReturnsPropertySymbol()
    {
        const string code = @"
class C
{
    int Prop { get; set; }
    void M()
    {
        System.Func<C, int> f = c => { return c.Prop; };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IPropertySymbol>(result);
        Assert.Equal("Prop", result!.Name);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_BlockLambdaWithMethodReturn_ReturnsMethodSymbol()
    {
        const string code = @"
class C
{
    int GetValue() => 42;
    void M()
    {
        System.Func<C, int> f = c => { return c.GetValue(); };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IMethodSymbol>(result);
        Assert.Equal("GetValue", result!.Name);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_BlockLambdaWithFieldReturn_ReturnsFieldSymbol()
    {
        const string code = @"
class C
{
    int _field;
    void M()
    {
        System.Func<C, int> f = c => { return c._field; };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IFieldSymbol>(result);
        Assert.Equal("_field", result!.Name);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_BlockLambdaWithEventReturn_ReturnsEventSymbol()
    {
        const string code = @"
class C
{
    event System.EventHandler MyEvent;
    void M()
    {
        System.Func<C, System.EventHandler> f = c => { return c.MyEvent; };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEventSymbol>(result);
        Assert.Equal("MyEvent", result!.Name);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_ExpressionLambdaWithFieldReference_ReturnsFieldSymbol()
    {
        const string code = @"
class C
{
    int _field;
    void M()
    {
        System.Func<C, int> f = c => c._field;
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IFieldSymbol>(result);
        Assert.Equal("_field", result!.Name);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_UnrecognizedOperationType_ReturnsNull()
    {
        const string code = @"
class C
{
    void M()
    {
        System.Func<int> f = () => 42;
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.Null(result);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_NullOperation_ReturnsNull()
    {
        IOperation? nullOperation = null;

        SyntaxNode? result = nullOperation.GetReferencedMemberSyntaxFromLambda();

        Assert.Null(result);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_ExpressionLambdaWithPropertyReference_ReturnsSyntax()
    {
        const string code = @"
class C
{
    int Prop { get; set; }
    void M()
    {
        System.Func<C, int> f = c => c.Prop;
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.NotNull(result);
        Assert.Contains("Prop", result!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_ExpressionLambdaWithMethodInvocation_ReturnsSyntax()
    {
        const string code = @"
class C
{
    int GetValue() => 42;
    void M()
    {
        System.Func<C, int> f = c => c.GetValue();
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.NotNull(result);
        Assert.Contains("GetValue", result!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_BlockLambdaWithPropertyReturn_ReturnsSyntax()
    {
        const string code = @"
class C
{
    int Prop { get; set; }
    void M()
    {
        System.Func<C, int> f = c => { return c.Prop; };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.NotNull(result);
        Assert.Contains("Prop", result!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_BlockLambdaWithMethodReturn_ReturnsSyntax()
    {
        const string code = @"
class C
{
    int GetValue() => 42;
    void M()
    {
        System.Func<C, int> f = c => { return c.GetValue(); };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.NotNull(result);
        Assert.Contains("GetValue", result!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_BlockLambdaWithEventReturn_ReturnsSyntax()
    {
        const string code = @"
class C
{
    event System.EventHandler MyEvent;
    void M()
    {
        System.Func<C, System.EventHandler> f = c => { return c.MyEvent; };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.NotNull(result);
        Assert.Contains("MyEvent", result!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_BlockLambdaWithFieldReturn_ReturnsSyntax()
    {
        const string code = @"
class C
{
    int _field;
    void M()
    {
        System.Func<C, int> f = c => { return c._field; };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.NotNull(result);
        Assert.Contains("_field", result!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_UnrecognizedOperationType_ReturnsNull()
    {
        const string code = @"
class C
{
    void M()
    {
        System.Func<int> f = () => 42;
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.Null(result);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_BlockLambdaWithMultipleOperations_ReturnsNull()
    {
        // Action block lambdas produce 2 operations (ExpressionStatement + implicit Return).
        // The method only handles block lambdas with exactly 1 operation.
        const string code = @"
class C
{
    int Prop { get; set; }
    void M()
    {
        System.Action<C> f = c => { c.Prop = 1; };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.Null(result);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_BlockLambdaWithMultipleOperations_ReturnsNull()
    {
        const string code = @"
class C
{
    int Prop { get; set; }
    void M()
    {
        System.Action<C> f = c => { c.Prop = 1; };
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.Null(result);
    }

    [Fact]
    public void GetReferencedMemberSymbolFromLambda_ExpressionLambdaWithEventReference_ReturnsEventSymbol()
    {
        const string code = @"
class C
{
    event System.EventHandler MyEvent;
    void M()
    {
        System.Func<C, System.EventHandler> f = c => c.MyEvent;
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        ISymbol? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSymbolFromLambda();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEventSymbol>(result);
        Assert.Equal("MyEvent", result!.Name);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_ExpressionLambdaWithEventReference_ReturnsSyntax()
    {
        const string code = @"
class C
{
    event System.EventHandler MyEvent;
    void M()
    {
        System.Func<C, System.EventHandler> f = c => c.MyEvent;
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.NotNull(result);
        Assert.Contains("MyEvent", result!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetReferencedMemberSyntaxFromLambda_ExpressionLambdaWithFieldReference_ReturnsSyntax()
    {
        const string code = @"
class C
{
    int _field;
    void M()
    {
        System.Func<C, int> f = c => c._field;
    }
}";
        IAnonymousFunctionOperation funcOp = GetLambdaOperation(code);

        SyntaxNode? result = funcOp.Body.WalkDownConversion().GetReferencedMemberSyntaxFromLambda();

        Assert.NotNull(result);
        Assert.Contains("_field", result!.ToString(), StringComparison.Ordinal);
    }

    private static (SemanticModel Model, SyntaxTree Tree) CreateCompilation(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            CoreReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        return (model, tree);
    }

    private static IAnonymousFunctionOperation GetLambdaOperation(string code)
    {
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
        LambdaExpressionSyntax lambda = GetFirstLambda(tree);
        IOperation? lambdaOp = model.GetOperation(lambda);
        Assert.NotNull(lambdaOp);
        IAnonymousFunctionOperation funcOp = (IAnonymousFunctionOperation)lambdaOp!;
        return funcOp;
    }

    private static T GetFirstOperationOfType<T>(string code)
        where T : IOperation
    {
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
        IEnumerable<IOperation> allOperations = tree.GetRoot()
            .DescendantNodes()
            .Select(node => model.GetOperation(node))
            .Where(op => op != null)!;

        T? found = allOperations.OfType<T>().FirstOrDefault();
        Assert.NotNull(found);
        return found!;
    }

    private static IEnumerable<IOperation> Flatten(IOperation operation)
    {
        yield return operation;
        foreach (IOperation child in operation.ChildOperations)
        {
            foreach (IOperation descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    private static LambdaExpressionSyntax GetFirstLambda(SyntaxTree tree)
    {
        ParenthesizedLambdaExpressionSyntax? pLambda = tree.GetRoot()
            .DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().FirstOrDefault();
        SimpleLambdaExpressionSyntax? sLambda = tree.GetRoot()
            .DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().FirstOrDefault();
        LambdaExpressionSyntax result = (LambdaExpressionSyntax?)pLambda ?? sLambda!;
        Assert.NotNull(result);
        return result;
    }
}
