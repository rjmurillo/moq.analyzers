using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Detects when <c>Mock&lt;T&gt;</c> is used where <c>T</c> is an <see langword="internal"/> type
/// and the assembly containing <c>T</c> does not have
/// <c>[InternalsVisibleTo("DynamicProxyGenAssembly2")]</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InternalTypeMustHaveInternalsVisibleToAnalyzer : DiagnosticAnalyzer
{
    private static readonly string DynamicProxyAssemblyName = "DynamicProxyGenAssembly2";

    private static readonly LocalizableString Title = "Moq: Internal type requires InternalsVisibleTo";
    private static readonly LocalizableString Message = "Internal type '{0}' requires [InternalsVisibleTo(\"DynamicProxyGenAssembly2\")] in its assembly to be mocked";
    private static readonly LocalizableString Description = "Mocking internal types requires the assembly to grant access to Castle DynamicProxy via InternalsVisibleTo.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.InternalTypeMustHaveInternalsVisibleTo,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.InternalTypeMustHaveInternalsVisibleTo}.md");

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

        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

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

        if (context.Operation is IObjectCreationOperation creation &&
            IsValidMockCreation(creation, knownSymbols, out mockedType))
        {
            diagnosticLocation = GetDiagnosticLocation(context.Operation, creation.Syntax);
        }
        else if (context.Operation is IInvocationOperation invocation &&
                 IsValidMockInvocation(invocation, knownSymbols, out mockedType))
        {
            diagnosticLocation = GetDiagnosticLocation(context.Operation, invocation.Syntax);
        }
        else
        {
            return;
        }

        if (mockedType != null && diagnosticLocation != null && ShouldReportDiagnostic(mockedType))
        {
            context.ReportDiagnostic(diagnosticLocation.CreateDiagnostic(Rule, mockedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }
    }

    private static bool IsValidMockCreation(
        IObjectCreationOperation creation,
        MoqKnownSymbols knownSymbols,
        [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (creation.Type is null || creation.Constructor is null || !creation.Type.IsInstanceOf(knownSymbols.Mock1))
        {
            return false;
        }

        return TryGetMockedTypeFromGeneric(creation.Type, out mockedType);
    }

    private static bool IsValidMockInvocation(
        IInvocationOperation invocation,
        MoqKnownSymbols knownSymbols,
        [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        IMethodSymbol targetMethod = invocation.TargetMethod;

        // Mock.Of<T>()
        if (IsMockOfMethod(targetMethod, knownSymbols))
        {
            if (targetMethod.TypeArguments.Length == 1)
            {
                mockedType = targetMethod.TypeArguments[0];
                return true;
            }

            return false;
        }

        // MockRepository.Create<T>()
        if (IsMockRepositoryCreateMethod(targetMethod, knownSymbols))
        {
            if (targetMethod.TypeArguments.Length == 1)
            {
                mockedType = targetMethod.TypeArguments[0];
                return true;
            }

            return false;
        }

        return false;
    }

    private static bool IsMockOfMethod(IMethodSymbol targetMethod, MoqKnownSymbols knownSymbols)
    {
        if (!targetMethod.IsStatic)
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

    private static bool IsMockRepositoryCreateMethod(IMethodSymbol targetMethod, MoqKnownSymbols knownSymbols)
    {
        return targetMethod.IsInstanceOf(knownSymbols.MockRepositoryCreate);
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

    /// <summary>
    /// Determines whether the mocked type is internal and its assembly lacks InternalsVisibleTo for DynamicProxy.
    /// </summary>
    private static bool ShouldReportDiagnostic(ITypeSymbol mockedType)
    {
        if (!IsEffectivelyInternal(mockedType))
        {
            return false;
        }

        return !HasInternalsVisibleToDynamicProxy(mockedType.ContainingAssembly);
    }

    /// <summary>
    /// Checks if the type has <see langword="internal"/> effective accessibility.
    /// A nested public type inside an internal type is also effectively internal.
    /// </summary>
    private static bool IsEffectivelyInternal(ITypeSymbol type)
    {
        ITypeSymbol? current = type;
        while (current != null)
        {
            if (current.DeclaredAccessibility == Accessibility.Internal ||
                current.DeclaredAccessibility == Accessibility.Friend)
            {
                return true;
            }

            current = current.ContainingType;
        }

        return false;
    }

    private static bool HasInternalsVisibleToDynamicProxy(IAssemblySymbol? assembly)
    {
        if (assembly is null)
        {
            return false;
        }

        foreach (AttributeData attribute in assembly.GetAttributes())
        {
            if (attribute.AttributeClass is null)
            {
                continue;
            }

            if (!string.Equals(
                    attribute.AttributeClass.ToDisplayString(),
                    "System.Runtime.CompilerServices.InternalsVisibleToAttribute",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is string assemblyName &&
                AssemblyNameStartsWithDynamicProxy(assemblyName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the assembly name starts with the DynamicProxy assembly name.
    /// The InternalsVisibleTo attribute value can include a public key, so we
    /// check for a prefix match rather than an exact match.
    /// </summary>
    private static bool AssemblyNameStartsWithDynamicProxy(string assemblyName)
    {
        return assemblyName.StartsWith(DynamicProxyAssemblyName, StringComparison.Ordinal);
    }

    private static Location GetDiagnosticLocation(IOperation operation, SyntaxNode fallbackSyntax)
    {
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
