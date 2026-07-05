namespace Moq.Analyzers.Test.Common;

public class MockBehaviorConstantValuesTests
{
    [Fact]
    public void ConstantValueEquals_ReturnsFalse_WhenKnownBehaviorFieldIsNull()
    {
        IFieldSymbol defaultBehavior = GetEnumField("Default");

        Assert.False(ConstantValueEquals(new Optional<object?>(defaultBehavior.ConstantValue), null));
    }

    [Fact]
    public void ConstantValueEquals_ReturnsFalse_WhenOperandHasNoValue()
    {
        IFieldSymbol strict = GetEnumField("Strict");

        Assert.False(ConstantValueEquals(default, strict));
    }

    [Fact]
    public void ConstantValueEquals_ReturnsTrue_WhenBoxedValuesAreEqual()
    {
        IFieldSymbol strict = GetEnumField("Strict");

        Assert.True(ConstantValueEquals(new Optional<object?>(strict.ConstantValue), strict));
    }

    [Fact]
    public void ConstantValueEquals_ReturnsFalse_WhenBoxedValuesDiffer()
    {
        IFieldSymbol defaultBehavior = GetEnumField("Default");
        IFieldSymbol strict = GetEnumField("Strict");

        Assert.False(ConstantValueEquals(new Optional<object?>(defaultBehavior.ConstantValue), strict));
    }

    [Fact]
    public void ConstantValueEquals_ReturnsTrue_WhenBothConstantsAreNull()
    {
        IFieldSymbol nullField = GetStringField("Value");

        Assert.True(ConstantValueEquals(new Optional<object?>(null), nullField));
    }

    private static bool ConstantValueEquals(Optional<object?> operandConstant, IFieldSymbol? knownBehaviorField)
    {
        Type helperType = typeof(Moq.Analyzers.SetStrictMockBehaviorAnalyzer).Assembly.GetType(
            "Moq.Analyzers.MockBehaviorConstantValues",
            throwOnError: true)!;
        System.Reflection.MethodInfo method = helperType.GetMethod(
            nameof(ConstantValueEquals),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

#pragma warning disable ECS0900 // Reflection invocation boxes the Optional<object?> argument; unavoidable and confined to test code.
        object? result = method.Invoke(null, [operandConstant, knownBehaviorField]);
#pragma warning restore ECS0900
        return Assert.IsType<bool>(result);
    }

    private static IFieldSymbol GetEnumField(string fieldName)
    {
        const string code = """
            internal enum Behavior
            {
                Default = 1,
                Strict = 2,
            }
            """;

        (SemanticModel model, _) = CompilationHelper.CreateCompilation(code);
        INamedTypeSymbol? behavior = model.Compilation.GetTypeByMetadataName("Behavior");
        Assert.NotNull(behavior);

        return behavior!.GetMembers(fieldName).OfType<IFieldSymbol>().Single();
    }

    private static IFieldSymbol GetStringField(string fieldName)
    {
        const string code = """
            internal static class Constants
            {
                public const string Value = null;
            }
            """;

        (SemanticModel model, _) = CompilationHelper.CreateCompilation(code);
        INamedTypeSymbol? constants = model.Compilation.GetTypeByMetadataName("Constants");
        Assert.NotNull(constants);

        return constants!.GetMembers(fieldName).OfType<IFieldSymbol>().Single();
    }
}
