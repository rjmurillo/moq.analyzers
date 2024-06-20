using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Benchmarks.Helpers;

internal class EmptyVerifier : IVerifier
{
    public void Empty<T>(string collectionName, IEnumerable<T> collection)
    {
        //throw new NotImplementedException();
    }

    public void Equal<T>(T expected, T actual, string? message = null)
    {
        //throw new NotImplementedException();
    }

    public void Fail(string? message = null)
    {
        //throw new NotImplementedException();
    }

    public void False([DoesNotReturnIf(true)] bool assert, string? message = null)
    {
        //throw new NotImplementedException();
    }

    public void LanguageIsSupported(string language)
    {
        //throw new NotImplementedException();
    }

    public void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
    {
        //throw new NotImplementedException();
    }

    public IVerifier PushContext(string context)
    {
        return this;
        //throw new NotImplementedException();
    }

    public void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null)
    {
        //throw new NotImplementedException();
    }

    public void True([DoesNotReturnIf(false)] bool assert, string? message = null)
    {
        //throw new NotImplementedException();
    }
}
