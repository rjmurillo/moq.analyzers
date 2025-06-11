namespace Moq.Analyzers.Test.Helpers;

internal static class TestDataExtensions
{
    /// <summary>
    /// Adds a namespace to each test case.
    /// </summary>
    /// <param name="data">The test data to extend.</param>
    /// <returns>The test data with a namespace added to each test case.</returns>
    public static IEnumerable<object[]> WithNamespaces(this IEnumerable<object[]> data)
    {
        foreach (object[] item in data)
        {
            yield return item.Prepend(string.Empty).ToArray();
            yield return item.Prepend("namespace MyNamespace;").ToArray();
        }
    }

    /// <summary>
    /// Adds the reference assembly group for .NET 8.0 with an older version of Moq (4.8.2) and a newer version of Moq (4.18.4) to each test case.
    /// </summary>
    /// <param name="data">The test data to extend.</param>
    /// <returns>The test data with the old and new Moq reference assembly groups added to each test case.</returns>
    public static IEnumerable<object[]> WithMoqReferenceAssemblyGroups(this IEnumerable<object[]> data)
    {
        foreach (object[] item in data)
        {
            yield return item.Prepend(ReferenceAssemblyCatalog.Net80WithOldMoq).ToArray();
            yield return item.Prepend(ReferenceAssemblyCatalog.Net80WithNewMoq).ToArray();
        }
    }

    /// <summary>
    /// Adds the reference assembly group for .NET 8.0 with an older version of Moq (4.8.2) to each test case.
    /// </summary>
    /// <param name="data">The test data to extend.</param>
    /// <returns>The test data with the old Moq reference assembly group added to each test case.</returns>
    public static IEnumerable<object[]> WithOldMoqReferenceAssemblyGroups(this IEnumerable<object[]> data)
    {
        foreach (object[] item in data)
        {
            yield return item.Prepend(ReferenceAssemblyCatalog.Net80WithOldMoq).ToArray();
        }
    }

    /// <summary>
    /// Adds the reference assembly group for .NET 8.0 with a newer version of Moq (4.18.4) to each test case.
    /// </summary>
    /// <param name="data">The test data to extend.</param>
    /// <returns>The test data with the old Moq reference assembly group added to each test case.</returns>
    public static IEnumerable<object[]> WithNewMoqReferenceAssemblyGroups(this IEnumerable<object[]> data)
    {
        foreach (object[] item in data)
        {
            yield return item.Prepend(ReferenceAssemblyCatalog.Net80WithNewMoq).ToArray();
        }
    }
}
