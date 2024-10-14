namespace Moq.Analyzers.Test.Helpers;

internal static class TestDataExtensions
{
    public static IEnumerable<object[]> WithNamespaces(this IEnumerable<object[]> data)
    {
        foreach (object[] item in data)
        {
            yield return item.Prepend(string.Empty).ToArray();
            yield return item.Prepend("namespace MyNamespace;").ToArray();
        }
    }

    public static IEnumerable<object[]> WithMoqReferenceAssemblyGroups(this IEnumerable<object[]> data)
    {
        foreach (object[] item in data)
        {
            yield return item.Prepend(ReferenceAssemblyCatalog.Net80WithOldMoq).ToArray();
            yield return item.Prepend(ReferenceAssemblyCatalog.Net80WithNewMoq).ToArray();
        }
    }

    public static IEnumerable<object[]> WithOldMoqReferenceAssemblyGroups(this IEnumerable<object[]> data)
    {
        foreach (object[] item in data)
        {
            yield return item.Prepend(ReferenceAssemblyCatalog.Net80WithOldMoq).ToArray();
        }
    }

    public static IEnumerable<object[]> WithNewMoqReferenceAssemblyGroups(this IEnumerable<object[]> data)
    {
        foreach (object[] item in data)
        {
            yield return item.Prepend(ReferenceAssemblyCatalog.Net80WithNewMoq).ToArray();
        }
    }
}
