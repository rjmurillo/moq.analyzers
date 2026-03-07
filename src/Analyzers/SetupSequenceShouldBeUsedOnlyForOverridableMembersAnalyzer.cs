using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// SetupSequence should be used only for overridable members.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid SetupSequence parameter";
    private static readonly LocalizableString Message = "SetupSequence should be used only for overridable members, but '{0}' is not overridable";
    private static readonly LocalizableString Description = "SetupSequence should be used only for overridable members.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetupSequenceOnlyUsedForOverridableMembers,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetupSequenceOnlyUsedForOverridableMembers}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Should be fixed. Ignoring for now to avoid additional churn as part of larger refactor.")]
    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        SemanticModel? semanticModel = invocationOperation.SemanticModel;

        if (semanticModel == null)
        {
            return;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);
        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        // 1. Check if the invoked method is a Moq SetupSequence method
        if (!targetMethod.IsMoqSetupSequenceMethod(knownSymbols))
        {
            return;
        }

        // 2. Attempt to locate the member reference from the SetupSequence expression argument.
        //    Typically, Moq SetupSequence calls have a single lambda argument like x => x.SomeMember.
        //    We'll extract that member reference or invocation to see whether it is overridable.
        ISymbol? mockedMemberSymbol = MoqVerificationHelpers.TryGetMockedMemberSymbol(invocationOperation);
        if (mockedMemberSymbol == null)
        {
            return;
        }

        // 3. Skip if the symbol is part of an interface, those are always "overridable".
        if (mockedMemberSymbol.ContainingType?.TypeKind == TypeKind.Interface)
        {
            return;
        }

        // 4. Check if symbol is a property or method, and if it is overridable or is returning a Task (which Moq allows).
        if (mockedMemberSymbol.IsOverridableOrAllowedMockMember(knownSymbols))
        {
            return;
        }

        // 5. If we reach here, the member is neither overridable nor allowed by Moq
        //    So we report the diagnostic.
        //
        // Try to get the specific member syntax for a more precise diagnostic location
        SyntaxNode? memberSyntax = MoqVerificationHelpers.TryGetMockedMemberSyntax(invocationOperation);
        Location diagnosticLocation = memberSyntax?.GetLocation() ?? invocationOperation.Syntax.GetLocation();

        Diagnostic diagnostic = diagnosticLocation.CreateDiagnostic(Rule, mockedMemberSymbol.ToDisplayString());
        context.ReportDiagnostic(diagnostic);
    }
}
