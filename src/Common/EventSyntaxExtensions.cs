// Copyright (c) 2025 Moq.Analyzers contributors
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.Analyzers.Common;

/// <summary>
/// Provides extension methods for analyzing event syntax.
/// </summary>
public static class EventSyntaxExtensions
{
    /// <summary>
    /// Attempts to extract the event type from an event selector expression.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventSelector">The event selector expression.</param>
    /// <param name="eventType">The extracted event type, if found.</param>
    /// <returns><c>true</c> if the event type was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetEventTypeFromSelector(
        SemanticModel semanticModel,
        ExpressionSyntax eventSelector,
        out ITypeSymbol? eventType)
    {
        eventType = null;
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(eventSelector);
        if (symbolInfo.Symbol is IEventSymbol eventSymbol)
        {
            eventType = eventSymbol.Type;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to extract the event symbol and handler type from setup arguments.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="arguments">The setup arguments.</param>
    /// <param name="eventSymbol">The extracted event symbol, if found.</param>
    /// <param name="handlerType">The extracted handler type, if found.</param>
    /// <returns><c>true</c> if both were found; otherwise, <c>false</c>.</returns>
    public static bool TryGetEventAndHandlerFromSetup(
        SemanticModel semanticModel,
        ArgumentSyntax[] arguments,
        out IEventSymbol? eventSymbol,
        out ITypeSymbol? handlerType)
    {
        eventSymbol = null;
        handlerType = null;

        if (arguments.Length < 2)
        {
            return false;
        }

        ExpressionSyntax eventSelector = arguments[0].Expression;
        ExpressionSyntax handler = arguments[1].Expression;

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(eventSelector);
        if (symbolInfo.Symbol is IEventSymbol evt)
        {
            eventSymbol = evt;
            handlerType = semanticModel.GetTypeInfo(handler).Type;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extracts the event type from a lambda selector of the form: x => x.EventName += null.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventSelector">The lambda event selector expression.</param>
    /// <param name="eventType">The extracted event type, if found.</param>
    /// <returns><c>true</c> if the event type was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetEventTypeFromLambdaSelector(
        SemanticModel semanticModel,
        ExpressionSyntax eventSelector,
        out ITypeSymbol? eventType)
    {
        eventType = null;

        // The event selector should be a lambda like: p => p.EventName += null
        if (eventSelector is not LambdaExpressionSyntax lambda)
        {
            return false;
        }

        // The body should be an assignment expression with += operator
        if (lambda.Body is not AssignmentExpressionSyntax assignment ||
            !assignment.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken))
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
        return true;
    }
}
