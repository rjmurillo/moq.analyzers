// Copyright (c) 2025 Moq.Analyzers contributors
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.Analyzers.Common
{
    public static class EventSyntaxExtensions
    {
        public static bool TryGetEventTypeFromSelector(
            SemanticModel semanticModel,
            ExpressionSyntax eventSelector,
            out ITypeSymbol? eventType)
        {
            eventType = null;
            var symbolInfo = semanticModel.GetSymbolInfo(eventSelector);
            if (symbolInfo.Symbol is IEventSymbol eventSymbol)
            {
                eventType = eventSymbol.Type;
                return true;
            }
            return false;
        }

        public static bool TryGetEventAndHandlerFromSetup(
            SemanticModel semanticModel,
            ArgumentSyntax[] arguments,
            out IEventSymbol? eventSymbol,
            out ITypeSymbol? handlerType)
        {
            eventSymbol = null;
            handlerType = null;

            if (arguments.Length < 2)
                return false;

            var eventSelector = arguments[0].Expression;
            var handler = arguments[1].Expression;

            var symbolInfo = semanticModel.GetSymbolInfo(eventSelector);
            if (symbolInfo.Symbol is IEventSymbol evt)
            {
                eventSymbol = evt;
                handlerType = semanticModel.GetTypeInfo(handler).Type;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Extracts the event type from a lambda selector of the form: x => x.EventName += null
        /// </summary>
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
            var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol is not IEventSymbol eventSymbol)
            {
                return false;
            }

            eventType = eventSymbol.Type;
            return true;
        }
    }
}
