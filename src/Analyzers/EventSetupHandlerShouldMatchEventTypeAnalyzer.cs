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

    private static bool TryGetEventAndHandlerFromSetup(
        SemanticModel semanticModel,
        ExpressionSyntax setupExpression,
        out ITypeSymbol? eventType,
        out ExpressionSyntax? handlerExpression)
    {
        eventType = null;
        handlerExpression = null;
        bool result = true;

        // The setup expression should be a lambda like: x => x.EventName += handler
        LambdaExpressionSyntax? lambda = setupExpression as LambdaExpressionSyntax;
        if (lambda == null)
        {
            result = false;
        }
        else
        {
            AssignmentExpressionSyntax? assignment = lambda.Body as AssignmentExpressionSyntax;
            if (assignment == null)
            {
                result = false;
            }
            else
            {
                bool isAddOrRemove = assignment.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken)
                                 || assignment.OperatorToken.IsKind(SyntaxKind.MinusEqualsToken);

                if (!isAddOrRemove)
                {
                    result = false;
                }
                else
                {
                    MemberAccessExpressionSyntax? memberAccess = assignment.Left as MemberAccessExpressionSyntax;
                    if (memberAccess == null)
                    {
                        result = false;
                    }
                    else
                    {
                        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                        IEventSymbol? eventSymbol = symbolInfo.Symbol as IEventSymbol;
                        if (eventSymbol == null)
                        {
                            result = false;
                        }
                        else
                        {
                            eventType = eventSymbol.Type;
                            handlerExpression = assignment.Right;
                        }
                    }
                }
            }
        }

        return result;
    }

    private static void ValidateHandlerType(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols, ExpressionSyntax handlerExpression, ITypeSymbol expectedEventType)
    {
        // Get the handler type from the expression
        if (!TryGetHandlerTypeFromExpression(context.SemanticModel, knownSymbols, handlerExpression, out ITypeSymbol? handlerType))
        {
            return;
        }

        // Check if the handler type matches the expected event delegate type
        if (!context.SemanticModel.HasConversion(handlerType!, expectedEventType))
        {
            // NOTE: This diagnostic is reported when the handler type doesn't match the event delegate type.
            // This code path is difficult to test in unit tests because most type mismatches that would
            // reach this point also cause compiler errors (CS0029) which prevent the analyzer from running.
            // The analyzer only runs on syntactically and semantically valid C# code from the compiler's
            // perspective, but can still detect Moq-specific semantic issues like delegate type mismatches.
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
                // NOTE: This code path extracts the generic type argument T from It.IsAny<T>() expressions.
                // It's difficult to test directly because most type mismatches that would exercise this
                // path also cause compiler errors before the analyzer runs. The analyzer is designed to
                // catch subtle Moq-specific type compatibility issues that pass C# compiler validation
                // but are semantically incorrect for event handler assignment.
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
}
