using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Moq.Analyzers;

internal abstract class MoqMethodDescriptorBase
{
    public abstract bool IsMatch(SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccessSyntax, CancellationToken cancellationToken);
}
