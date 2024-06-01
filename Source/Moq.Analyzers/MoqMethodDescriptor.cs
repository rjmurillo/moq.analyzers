using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.Analyzers
{
    internal class MoqMethodDescriptor
    {
        private readonly bool isGeneric;

        public MoqMethodDescriptor(string shortMethodName, Regex fullMethodNamePattern, bool isGeneric = false)
        {
            this.isGeneric = isGeneric;
            ShortMethodName = shortMethodName;
            FullMethodNamePattern = fullMethodNamePattern;
        }

        private string ShortMethodName { get; }

        private Regex FullMethodNamePattern { get; }

        public bool IsMoqMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
        {
            var methodName = method?.Name.ToString();

            // First fast check before walking semantic model
            if (DoesShortMethodMatch(methodName) == false) return false;

            var symbolInfo = semanticModel.GetSymbolInfo(method);
            if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
            {
                return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>()
                    .Any(s => this.FullMethodNamePattern.IsMatch(s.ToString()));
            }
            else if (symbolInfo.CandidateReason == CandidateReason.None)
            {
                // TODO: Replace regex with something more elegant
                return symbolInfo.Symbol is IMethodSymbol &&
                       this.FullMethodNamePattern.IsMatch(symbolInfo.Symbol.ToString());
            }

            return false;
        }

        private bool DoesShortMethodMatch(string methodName)
        {
            if (isGeneric)
            {
                return methodName.StartsWith($"{this.ShortMethodName}<");
            }
            return methodName == this.ShortMethodName;
        }
    }
}
