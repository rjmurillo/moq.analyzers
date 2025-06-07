using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Sealed classes cannot be mocked.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoSealedClassMocksAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Sealed class mocked";
    private static readonly LocalizableString Message = "Sealed classes cannot be mocked";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SealedClassCannotBeMocked,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SealedClassCannotBeMocked}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Check that Mock<T> type is available
        if (knownSymbols.Mock1 is null)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, knownSymbols),
            OperationKind.ObjectCreation,
            OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        ITypeSymbol? mockedType = null;
        Location? diagnosticLocation = null;

        // Handle object creation: new Mock<T>()
        if (context.Operation is IObjectCreationOperation creation &&
            IsValidMockCreation(creation, knownSymbols, out mockedType))
        {
            diagnosticLocation = GetDiagnosticLocationForObjectCreation(context.Operation, creation);
        }

        // Handle static method invocation: Mock.Of<T>()
        else if (context.Operation is IInvocationOperation invocation &&
                 IsValidMockOfInvocation(invocation, knownSymbols, out mockedType))
        {
            diagnosticLocation = GetDiagnosticLocationForInvocation(context.Operation, invocation);
        }

        if (mockedType != null && diagnosticLocation != null && ShouldReportDiagnostic(mockedType))
        {
            context.ReportDiagnostic(diagnosticLocation.CreateDiagnostic(Rule));
        }
    }

    private static bool IsValidMockCreation(IObjectCreationOperation creation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (creation.Type is null || creation.Constructor is null || !creation.Type.IsInstanceOf(knownSymbols.Mock1))
        {
            return false;
        }

        return TryGetMockedTypeFromGeneric(creation.Type, out mockedType);
    }

    private static bool IsValidMockOfInvocation(IInvocationOperation invocation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        // Check if this is a static method call to Mock.Of<T>()
        if (invocation.TargetMethod is null ||
            !invocation.TargetMethod.IsStatic ||
            !string.Equals(invocation.TargetMethod.Name, "Of", StringComparison.Ordinal) ||
            invocation.TargetMethod.ContainingType is null ||
            !invocation.TargetMethod.ContainingType.Equals(knownSymbols.Mock, SymbolEqualityComparer.Default))
        {
            return false;
        }

        // Get the type argument from Mock.Of<T>()
        if (invocation.TargetMethod.TypeArguments.Length == 1)
        {
            mockedType = invocation.TargetMethod.TypeArguments[0];
            return true;
        }

        return false;
    }

    private static bool TryGetMockedTypeFromGeneric(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (type is not INamedTypeSymbol namedType || namedType.TypeArguments.Length != 1)
        {
            return false;
        }

        mockedType = namedType.TypeArguments[0];
        return true;
    }

    private static bool ShouldReportDiagnostic(ITypeSymbol mockedType)
    {
        // Check if the mocked type is sealed (but allow delegates)
        // Note: All delegates in .NET are sealed by default, but they can still be mocked by Moq

        // For nullable reference types (T?), we don't consider them as sealed
        // because you're mocking the nullable wrapper, not the underlying sealed type
        if (mockedType.CanBeReferencedByName && mockedType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return false;
        }

        // Handle nullable value types (Nullable<T>)
        if (mockedType.OriginalDefinition?.SpecialType == SpecialType.System_Nullable_T)
        {
            return false;
        }

        return mockedType.IsSealed && mockedType.TypeKind != TypeKind.Delegate;
    }

    private static Location GetDiagnosticLocationForObjectCreation(IOperation operation, IObjectCreationOperation creation)
    {
        return GetDiagnosticLocation(operation, creation.Syntax);
    }

    private static Location GetDiagnosticLocationForInvocation(IOperation operation, IInvocationOperation invocation)
    {
        return GetDiagnosticLocation(operation, invocation.Syntax);
    }

    private static Location GetDiagnosticLocation(IOperation operation, SyntaxNode fallbackSyntax)
    {
        // Try to locate the type argument in the syntax tree to report the diagnostic at the correct location.
        // If that fails for any reason, report the diagnostic on the fallback syntax.
        TypeSyntax? typeArgument = operation.Syntax
            .DescendantNodes()
            .OfType<GenericNameSyntax>()
            .FirstOrDefault()?
            .TypeArgumentList?
            .Arguments
            .FirstOrDefault();

        return typeArgument?.GetLocation() ?? fallbackSyntax.GetLocation();
    }
}
