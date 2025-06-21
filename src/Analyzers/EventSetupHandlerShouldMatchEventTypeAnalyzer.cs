namespace Moq.Analyzers;

/// <summary>
/// Event setup handler type should match the event delegate type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventSetupHandlerShouldMatchEventTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Event setup handler type should match event delegate type";
    private static readonly LocalizableString Message = "Event setup handler type should match the event delegate type";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.EventSetupHandlerShouldMatchEventType,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.EventSetupHandlerShouldMatchEventType}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.SemanticModel.Compilation);
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a SetupAdd or SetupRemove method call on a Mock<T>
        if (!IsEventSetupMethodCall(context.SemanticModel, invocation, knownSymbols))
        {
            return;
        }

        if (!TryGetEventSetupArguments(invocation, context.SemanticModel, out ExpressionSyntax? handlerExpression, out ITypeSymbol? expectedEventType))
        {
            return;
        }

        ValidateHandlerType(context, knownSymbols, handlerExpression!, expectedEventType!);
    }

    private static bool IsEventSetupMethodCall(SemanticModel semanticModel, InvocationExpressionSyntax invocation, MoqKnownSymbols knownSymbols)
    {
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        return methodSymbol.IsMoqEventSetupMethod(knownSymbols);
    }

    private static bool TryGetEventSetupArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out ExpressionSyntax? handlerExpression, out ITypeSymbol? expectedEventType)
    {
        handlerExpression = null;
        expectedEventType = null;

        // Get the arguments to the SetupAdd/SetupRemove method
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        // SetupAdd/SetupRemove should have exactly 1 argument (the event setup lambda)
        if (arguments.Count != 1)
        {
            return false;
        }

        // First argument should be a lambda that sets up the event handler
        ExpressionSyntax setupExpression = arguments[0].Expression;
        if (!TryGetEventAndHandlerFromSetup(semanticModel, setupExpression, out ITypeSymbol? eventType, out ExpressionSyntax? handler))
        {
            return false;
        }

        expectedEventType = eventType;
        handlerExpression = handler;
        return true;
    }

    private static bool TryGetEventAndHandlerFromSetup(SemanticModel semanticModel, ExpressionSyntax setupExpression, out ITypeSymbol? eventType, out ExpressionSyntax? handlerExpression)
    {
        eventType = null;
        handlerExpression = null;

        // The setup expression should be a lambda like: x => x.EventName += handler
        if (setupExpression is not LambdaExpressionSyntax lambda)
        {
            return false;
        }

        // The body should be an assignment expression with += or -= operator
        if (lambda.Body is not AssignmentExpressionSyntax assignment ||
            (!assignment.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken) && !assignment.OperatorToken.IsKind(SyntaxKind.MinusEqualsToken)))
        {
            return false;
        }

        // The left side should be a member access to the event
        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // Get the symbol for the event
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is not IEventSymbol eventSymbol)
        {
            return false;
        }

        eventType = eventSymbol.Type;
        handlerExpression = assignment.Right;
        return true;
    }

    private static void ValidateHandlerType(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols, ExpressionSyntax handlerExpression, ITypeSymbol expectedEventType)
    {
        // Get the handler type from the expression
        if (!TryGetHandlerTypeFromExpression(context.SemanticModel, knownSymbols, handlerExpression, out ITypeSymbol? handlerType))
        {
            return;
        }

        // Check if the handler type matches the expected event delegate type
        if (!HasConversion(context.SemanticModel, handlerType!, expectedEventType))
        {
            // Report on the handler expression
            Diagnostic diagnostic = handlerExpression.GetLocation().CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool TryGetHandlerTypeFromExpression(SemanticModel semanticModel, MoqKnownSymbols knownSymbols, ExpressionSyntax handlerExpression, out ITypeSymbol? handlerType)
    {
        handlerType = null;

        // Handle It.IsAny<T>() expressions using semantic analysis
        if (handlerExpression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName &&
            genericName.TypeArgumentList.Arguments.Count == 1)
        {
            // Use semantic model to check if this is actually It.IsAny
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                knownSymbols.ItIsAny.Contains(methodSymbol))
            {
                // For It.IsAny<T>(), get T from the generic type arguments
                TypeInfo typeInfo = semanticModel.GetTypeInfo(genericName.TypeArgumentList.Arguments[0]);
                handlerType = typeInfo.Type;
                return handlerType != null;
            }
        }

        // For other expressions, get the type directly
        TypeInfo expressionTypeInfo = semanticModel.GetTypeInfo(handlerExpression);
        handlerType = expressionTypeInfo.Type;
        return handlerType != null;
    }

    private static bool HasConversion(SemanticModel semanticModel, ITypeSymbol source, ITypeSymbol destination)
    {
        return semanticModel.HasConversion(source, destination);
    }
}
