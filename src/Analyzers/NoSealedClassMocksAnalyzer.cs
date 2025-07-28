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
    private static readonly LocalizableString Message = "Sealed class '{0}' cannot be mocked";
    private static readonly LocalizableString Description = "Sealed classes cannot be mocked.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SealedClassCannotBeMocked,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
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

    /// <summary>
    /// Registers the operation action for object creation and invocation if Moq is referenced and Mock{T} is available.
    /// </summary>
    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Check that Mock{T} type is available
        if (knownSymbols.Mock1 is null)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, knownSymbols),
            OperationKind.ObjectCreation,
            OperationKind.Invocation);
    }

    /// <summary>
    /// Analyzes object creation and invocation operations to report diagnostics for sealed class mocks.
    /// </summary>
    private static void Analyze(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        ITypeSymbol? mockedType = null;
        Location? diagnosticLocation = null;

        // Handle object creation: new Mock{T}()
        if (context.Operation is IObjectCreationOperation creation &&
            IsValidMockCreation(creation, knownSymbols, out mockedType))
        {
            diagnosticLocation = GetDiagnosticLocationForObjectCreation(context.Operation, creation);
        }

        // Handle static method invocation: Mock.Of{T}()
        else if (context.Operation is IInvocationOperation invocation &&
                 IsValidMockOfInvocation(invocation, knownSymbols, out mockedType))
        {
            diagnosticLocation = GetDiagnosticLocationForInvocation(context.Operation, invocation);
        }
        else
        {
            // Operation is neither a Mock object creation nor a Mock.Of invocation that we need to analyze
            return;
        }

        if (mockedType != null && diagnosticLocation != null && ShouldReportDiagnostic(mockedType))
        {
            context.ReportDiagnostic(diagnosticLocation.CreateDiagnostic(Rule, mockedType.Name));
        }
    }

    /// <summary>
    /// Determines if the operation is a valid Mock{T} object creation and extracts the mocked type.
    /// </summary>
    private static bool IsValidMockCreation(IObjectCreationOperation creation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (creation.Type is null || creation.Constructor is null || !creation.Type.IsInstanceOf(knownSymbols.Mock1))
        {
            return false;
        }

        return TryGetMockedTypeFromGeneric(creation.Type, out mockedType);
    }

    /// <summary>
    /// Determines if the operation is a valid Mock.Of{T}() invocation and extracts the mocked type.
    /// </summary>
    private static bool IsValidMockOfInvocation(IInvocationOperation invocation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        // Check if this is a static method call to Mock.Of{T}()
        if (!IsValidMockOfMethod(invocation.TargetMethod, knownSymbols))
        {
            return false;
        }

        // Get the type argument from Mock.Of{T}()
        if (invocation.TargetMethod.TypeArguments.Length == 1)
        {
            mockedType = invocation.TargetMethod.TypeArguments[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the method symbol represents a static Mock.Of{T}() method.
    /// </summary>
    private static bool IsValidMockOfMethod(IMethodSymbol? targetMethod, MoqKnownSymbols knownSymbols)
    {
        if (targetMethod is null || !targetMethod.IsStatic)
        {
            return false;
        }

        if (!string.Equals(targetMethod.Name, "Of", StringComparison.Ordinal))
        {
            return false;
        }

        return targetMethod.ContainingType is not null &&
               targetMethod.ContainingType.Equals(knownSymbols.Mock, SymbolEqualityComparer.Default);
    }

    /// <summary>
    /// Attempts to extract the mocked type argument from a generic Mock{T} type.
    /// </summary>
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

    /// <summary>
    /// Determines whether a diagnostic should be reported for the mocked type based on its characteristics.
    /// </summary>
    /// <param name="mockedType">The type being mocked.</param>
    /// <returns>
    ///   Returns <see langword="true"/> when the mocked type is a sealed *reference* type (including nullable reference
    ///   types), <b>except arrays</b>. Returns <see langword="false"/> for delegates, arrays, and for all value types (structs / enums), including
    ///   <see cref="Nullable{T}"/>.
    /// </returns>
    private static bool ShouldReportDiagnostic(ITypeSymbol mockedType)
    {
        // Exclude delegates (all delegates are sealed, but Moq allows mocking them)
        if (mockedType.TypeKind == TypeKind.Delegate)
        {
            return false;
        }

        // Exclude nullable value types (Nullable{T})
        if (mockedType.OriginalDefinition?.SpecialType == SpecialType.System_Nullable_T)
        {
            return false;
        }

        // Exclude structs and enums
        if (mockedType.TypeKind == TypeKind.Struct || mockedType.TypeKind == TypeKind.Enum)
        {
            return false;
        }

        // For reference types, report if sealed
        return mockedType.IsSealed;
    }

    /// <summary>
    /// Gets the diagnostic location for a Mock{T} object creation.
    /// </summary>
    private static Location GetDiagnosticLocationForObjectCreation(IOperation operation, IObjectCreationOperation creation)
    {
        return GetDiagnosticLocation(operation, creation.Syntax);
    }

    /// <summary>
    /// Gets the diagnostic location for a Mock.Of{T}() invocation.
    /// </summary>
    private static Location GetDiagnosticLocationForInvocation(IOperation operation, IInvocationOperation invocation)
    {
        return GetDiagnosticLocation(operation, invocation.Syntax);
    }

    /// <summary>
    /// Attempts to locate the type argument in the syntax tree for precise diagnostic reporting.
    /// </summary>
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
