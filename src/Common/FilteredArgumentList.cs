using System.Runtime.InteropServices;

namespace Moq.Analyzers.Common;

/// <summary>
/// A zero-allocation view over a <see cref="SeparatedSyntaxList{TNode}"/> that
/// logically excludes one element by index. Avoids <c>ToArray()</c> and
/// <c>RemoveAt()</c> allocations when stripping a <c>MockBehavior</c> argument.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct FilteredArgumentList
{
    private readonly SeparatedSyntaxList<ArgumentSyntax> _arguments;
    private readonly int _skipIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilteredArgumentList"/> struct
    /// from an <see cref="ArgumentListSyntax"/>, optionally skipping one argument.
    /// </summary>
    /// <param name="argumentList">The argument list syntax node, or <see langword="null"/> for an empty list.</param>
    /// <param name="skipIndex">
    /// The index of the argument to exclude, or -1 to include all arguments.
    /// </param>
    public FilteredArgumentList(ArgumentListSyntax? argumentList, int skipIndex)
    {
#pragma warning disable ECS1200 // Fields depend on constructor parameters; initializers are not viable
        _arguments = argumentList?.Arguments ?? default;
        _skipIndex = skipIndex >= 0 && skipIndex < _arguments.Count ? skipIndex : -1;
#pragma warning restore ECS1200
    }

    /// <summary>
    /// Gets the number of visible (non-skipped) arguments.
    /// </summary>
    public int Count => _skipIndex >= 0 ? _arguments.Count - 1 : _arguments.Count;

    /// <summary>
    /// Gets the argument at the specified logical index, accounting for the skipped element.
    /// </summary>
    /// <param name="index">The zero-based logical index.</param>
    /// <returns>The <see cref="ArgumentSyntax"/> at the logical position.</returns>
    public ArgumentSyntax this[int index]
    {
        get
        {
            int rawIndex = _skipIndex >= 0 && index >= _skipIndex ? index + 1 : index;
            return _arguments[rawIndex];
        }
    }

    /// <summary>
    /// Formats the visible arguments as a parenthesized, comma-separated string.
    /// </summary>
    /// <returns>A string such as <c>(arg1, arg2)</c> or <c>()</c> if empty.</returns>
    public string FormatArguments()
    {
        if (Count == 0)
        {
            return "()";
        }

        // For small argument lists (typical 0-5 items), StringBuilder is efficient enough.
        // Avoid LINQ/Select to eliminate delegate allocation.
        System.Text.StringBuilder sb = new();
        sb.Append('(');

        bool first = true;
        for (int i = 0; i < _arguments.Count; i++)
        {
            if (i == _skipIndex)
            {
                continue;
            }

            if (!first)
            {
                sb.Append(", ");
            }

            sb.Append(_arguments[i].Expression.ToString());
            first = false;
        }

        sb.Append(')');
        return sb.ToString();
    }
}
