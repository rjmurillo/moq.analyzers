using Microsoft.CodeAnalysis.Operations;
using Moq.Analyzers.Common;

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

    private static void Analyze(
        OperationAnalysisContext context,
        MoqKnownSymbols knownSymbols)
    {
        ITypeSymbol? mockedType = null;
        Location? diagnosticLocation = null;

        if (context.Operation is IObjectCreationOperation creation &&
            MockDetectionHelpers.IsValidMockCreation(creation, knownSymbols, out mockedType))
        {
            diagnosticLocation = MockDetectionHelpers.GetDiagnosticLocation(context.Operation, creation.Syntax);
        }
        else if (context.Operation is IInvocationOperation invocation &&
                 IsValidMockInvocation(invocation, knownSymbols, out mockedType))
        {
            diagnosticLocation = MockDetectionHelpers.GetDiagnosticLocation(context.Operation, invocation.Syntax);
        }
        else
        {
            return;
        }

        if (mockedType != null && diagnosticLocation != null &&
            ShouldReportDiagnostic(mockedType, knownSymbols.InternalsVisibleToAttribute))
        {
            context.ReportDiagnostic(diagnosticLocation.CreateDiagnostic(
                Rule,
                mockedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }
    }

    private static bool IsValidMockInvocation(
        IInvocationOperation invocation,
        MoqKnownSymbols knownSymbols,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        IMethodSymbol targetMethod = invocation.TargetMethod;

        // Mock.Of<T>() -- use symbol-based comparison via MoqKnownSymbols.MockOf
        if (targetMethod.IsInstanceOf(knownSymbols.MockOf))
        {
            if (targetMethod.TypeArguments.Length == 1)
            {
                mockedType = targetMethod.TypeArguments[0];
                return true;
            }

            return false;
        }

        // MockRepository.Create<T>()
        if (targetMethod.IsInstanceOf(knownSymbols.MockRepositoryCreate))
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

    /// <summary>
    /// Determines whether the mocked type is effectively internal and its assembly
    /// lacks InternalsVisibleTo for DynamicProxy.
    /// </summary>
    private static bool ShouldReportDiagnostic(
        ITypeSymbol mockedType,
        INamedTypeSymbol? internalsVisibleToAttribute)
    {
        if (!IsEffectivelyInternal(mockedType))
        {
            return false;
        }

        return !HasInternalsVisibleToDynamicProxy(mockedType.ContainingAssembly, internalsVisibleToAttribute);
    }

    /// <summary>
    /// Checks if the type (or any containing type) has accessibility that requires
    /// InternalsVisibleTo for DynamicProxy to access it. DynamicProxy resides in a
    /// separate assembly and does not derive from containing types, so it relies on
    /// assembly-level access. Any of the following accessibility levels on the type
    /// or its containers make it inaccessible to DynamicProxy without InternalsVisibleTo:
    /// <list type="bullet">
    /// <item><see cref="Accessibility.Internal"/> (internal)</item>
    /// <item><see cref="Accessibility.ProtectedAndInternal"/> (private protected)</item>
    /// <item><see cref="Accessibility.ProtectedOrInternal"/> (protected internal) on
    /// a containing type, because DynamicProxy does not derive from the container</item>
    /// <item><see cref="Accessibility.Private"/> on a nested type</item>
    /// </list>
    /// </summary>
    private static bool IsEffectivelyInternal(ITypeSymbol type)
    {
        ITypeSymbol? current = type;
        while (current != null)
        {
            switch (current.DeclaredAccessibility)
            {
                case Accessibility.Internal:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.Private:
                    return true;
            }

            current = current.ContainingType;
        }

        return false;
    }

    /// <summary>
    /// Checks the assembly's attributes for InternalsVisibleTo targeting DynamicProxy,
    /// using symbol-based comparison for the attribute type.
    /// </summary>
    private static bool HasInternalsVisibleToDynamicProxy(
        IAssemblySymbol? assembly,
        INamedTypeSymbol? internalsVisibleToAttribute)
    {
        if (assembly is null)
        {
            return false;
        }

        // If we cannot resolve InternalsVisibleToAttribute (highly unlikely), bail out
        // conservatively by not reporting a diagnostic (avoiding false positives).
        if (internalsVisibleToAttribute is null)
        {
            return true;
        }

        foreach (AttributeData attribute in assembly.GetAttributes())
        {
            if (attribute.AttributeClass is null)
            {
                continue;
            }

            // Symbol-based comparison instead of string-based ToDisplayString()
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, internalsVisibleToAttribute))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is string assemblyName &&
                IsDynamicProxyAssemblyName(assemblyName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the assembly name matches DynamicProxy. The InternalsVisibleTo attribute
    /// value can be either the simple name ("DynamicProxyGenAssembly2") or include a
    /// public key token ("DynamicProxyGenAssembly2, PublicKey=..."). We match the exact
    /// name followed by either end-of-string or a comma separator.
    /// </summary>
    private static bool IsDynamicProxyAssemblyName(string assemblyName)
    {
        if (!assemblyName.StartsWith(DynamicProxyAssemblyName, StringComparison.Ordinal))
        {
            return false;
        }

        // Must be exact match or followed by comma (for public key suffix)
        return assemblyName.Length == DynamicProxyAssemblyName.Length ||
               assemblyName[DynamicProxyAssemblyName.Length] == ',';
    }
}
