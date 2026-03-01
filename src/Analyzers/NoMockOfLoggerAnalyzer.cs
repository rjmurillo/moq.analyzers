using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// ILogger should not be mocked. Use NullLogger or FakeLogger instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoMockOfLoggerAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: ILogger mocked";
    private static readonly LocalizableString Message = "ILogger should not be mocked; use NullLogger<T>.Instance or FakeLogger from Microsoft.Extensions.Diagnostics.Testing instead";
    private static readonly LocalizableString Description = "Mocking ILogger is unnecessary and fragile. Use NullLogger<T>.Instance for tests that ignore logging, or FakeLogger from Microsoft.Extensions.Diagnostics.Testing for tests that verify log output.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.LoggerShouldNotBeMocked,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.LoggerShouldNotBeMocked}.md");

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
        if (!knownSymbols.IsMockReferenced()) { return; }
        if (knownSymbols.Mock1 is null) { return; }
        if (knownSymbols.ILogger is null && knownSymbols.ILogger1 is null) { return; }

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
                 IsValidMockOfInvocation(invocation, knownSymbols, out mockedType))
        {
            diagnosticLocation = GetDiagnosticLocation(context.Operation, invocation.Syntax);
        }
        else
        {
            return;
        }

        if (mockedType != null && diagnosticLocation != null && IsLoggerType(mockedType, knownSymbols))
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
        if (!IsValidMockOfMethod(invocation.TargetMethod, knownSymbols)) { return false; }
        if (invocation.TargetMethod.TypeArguments.Length == 1)
        {
            mockedType = invocation.TargetMethod.TypeArguments[0];
            return true;
        }

        return false;
    }

    private static bool IsValidMockOfMethod(IMethodSymbol? targetMethod, MoqKnownSymbols knownSymbols)
    {
        if (targetMethod is null || !targetMethod.IsStatic) { return false; }
        if (!string.Equals(targetMethod.Name, "Of", StringComparison.Ordinal)) { return false; }
        return targetMethod.ContainingType is not null &&
               targetMethod.ContainingType.Equals(knownSymbols.Mock, SymbolEqualityComparer.Default);
    }

    private static bool TryGetMockedTypeFromGeneric(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;
        if (type is not INamedTypeSymbol namedType || namedType.TypeArguments.Length != 1) { return false; }
        mockedType = namedType.TypeArguments[0];
        return true;
    }

    private static bool IsLoggerType(ITypeSymbol mockedType, MoqKnownSymbols knownSymbols)
    {
        if (mockedType is not INamedTypeSymbol namedType) { return false; }
        INamedTypeSymbol originalDefinition = namedType.OriginalDefinition;
        if (knownSymbols.ILogger is not null &&
            SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.ILogger))
        {
            return true;
        }

        if (knownSymbols.ILogger1 is not null &&
            SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.ILogger1))
        {
            return true;
        }

        return false;
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
