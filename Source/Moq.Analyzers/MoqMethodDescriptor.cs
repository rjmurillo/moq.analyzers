using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Moq.Analyzers;

internal class MoqMethodDescriptor
{
    private readonly bool _isGeneric;

    public MoqMethodDescriptor(string shortMethodName, Regex fullMethodNamePattern, bool isGeneric = false)
    {
        _isGeneric = isGeneric;
        ShortMethodName = shortMethodName;
        FullMethodNamePattern = fullMethodNamePattern;
    }

    private string ShortMethodName { get; }

    private Regex FullMethodNamePattern { get; }

    public bool IsMoqMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax? method)
    {
        var methodName = method?.Name.ToString();

        Debug.Assert(!string.IsNullOrEmpty(methodName), nameof(methodName) + " != null or empty");

        if (string.IsNullOrEmpty(methodName)) return false;

        // First fast check before walking semantic model
        if (!DoesShortMethodMatch(methodName!)) return false;

        Debug.Assert(method != null, nameof(method) + " != null");

        if (method == null)
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(method);
        return symbolInfo.CandidateReason switch
        {
            CandidateReason.OverloadResolutionFailure => symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Any(s => FullMethodNamePattern.IsMatch(s.ToString())),
            CandidateReason.None => symbolInfo.Symbol is IMethodSymbol &&
                                    FullMethodNamePattern.IsMatch(symbolInfo.Symbol.ToString()),
            _ => false,
        };
    }

    private bool DoesShortMethodMatch(string methodName)
    {
        if (_isGeneric)
        {
            return methodName.StartsWith($"{ShortMethodName}<", StringComparison.Ordinal);
        }

        return string.Equals(methodName, ShortMethodName, StringComparison.Ordinal);
    }
}
