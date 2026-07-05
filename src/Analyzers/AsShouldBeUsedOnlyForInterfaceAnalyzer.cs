using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock.As() should take interfaces only.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsShouldBeUsedOnlyForInterfaceAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid As type parameter";
    private static readonly LocalizableString Message = "Mock.As() should take interfaces only, but '{0}' is not an interface";
    private static readonly LocalizableString Description = "Mock.As() should take interfaces only.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AsShouldOnlyBeUsedForInterfacesRuleId,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.AsShouldOnlyBeUsedForInterfacesRuleId}.md");

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

        // Only analyze if Moq is referenced - if we're analyzing Moq usage patterns, Moq should be available
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Look for the Mock.As() method and provide it to Analyze to avoid looking it up multiple times.
        ImmutableArray<IMethodSymbol> asMethods = ImmutableArray.CreateRange([
            ..knownSymbols.MockAs,
            ..knownSymbols.Mock1As]);

        // If As() methods are not available, this may indicate an unsupported Moq version
        if (asMethods.IsEmpty)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, asMethods),
            OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, ImmutableArray<IMethodSymbol> wellKnownAsMethods)
    {
        // This should always be an invocation operation since we registered for OperationKind.Invocation
        Debug.Assert(context.Operation is IInvocationOperation, "Expected IInvocationOperation");

        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (!targetMethod.IsInstanceOf(wellKnownAsMethods))
        {
            return;
        }

        ImmutableArray<ITypeSymbol> typeArguments = targetMethod.TypeArguments;
        if (typeArguments.Length != 1)
        {
            return;
        }

        ITypeSymbol typeSymbol = typeArguments[0];

        // Interface: this is the valid, intended use of As<T>, so never report.
        // Error: the type failed to bind, so the code already has a compiler error; adding
        // Moq1300 on top is noise (issue #1251).
        if (typeSymbol.TypeKind is TypeKind.Interface or TypeKind.Error)
        {
            return;
        }

        // Open generic type parameter: at the call site T may be substituted with an interface,
        // so reporting is a false positive (issue #1251) UNLESS its constraints make an interface
        // substitution impossible (a value-type or base-class constraint). In that case As<T> can
        // never bind to an interface, so the diagnostic is correct.
        if (typeSymbol is ITypeParameterSymbol typeParameter && CanBeSubstitutedWithInterface(typeParameter))
        {
            return;
        }

        // Find the first As<T> generic type argument and report the diagnostic on it
        GenericNameSyntax? asGeneric = invocationOperation.Syntax
            .DescendantNodes()
            .OfType<GenericNameSyntax>()
            .FirstOrDefault(x => string.Equals(x.Identifier.ValueText, "As", StringComparison.Ordinal));

        TypeSyntax? typeArg = asGeneric?.TypeArgumentList.Arguments.FirstOrDefault();
        Location location = typeArg?.GetLocation() ?? invocationOperation.Syntax.GetLocation();
        context.ReportDiagnostic(location.CreateDiagnostic(Rule, typeSymbol.ToDisplayString()));
    }

    /// <summary>
    /// Determines whether an open generic type parameter could be substituted with an interface
    /// at a call site, given its declared constraints.
    /// </summary>
    /// <param name="typeParameter">The open generic type parameter used as the <c>As&lt;T&gt;</c> argument.</param>
    /// <returns>
    /// <see langword="false" /> when a value-type constraint (<c>struct</c>/<c>unmanaged</c>) or a
    /// base-class constraint forbids an interface substitution; otherwise <see langword="true" />.
    /// </returns>
    private static bool CanBeSubstitutedWithInterface(ITypeParameterSymbol typeParameter)
    {
        // A value type can never be an interface.
        if (typeParameter.HasValueTypeConstraint || typeParameter.HasUnmanagedTypeConstraint)
        {
            return false;
        }

        // A base-class constraint forces T to derive from a class, so it cannot be an interface.
        foreach (ITypeSymbol constraintType in typeParameter.ConstraintTypes)
        {
            if (constraintType.TypeKind == TypeKind.Class)
            {
                return false;
            }
        }

        return true;
    }
}
