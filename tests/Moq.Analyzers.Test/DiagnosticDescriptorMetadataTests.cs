using System.Reflection;

namespace Moq.Analyzers.Test;

public class DiagnosticDescriptorMetadataTests
{
    public static TheoryData<string, DiagnosticDescriptor> DescriptorData()
    {
        TheoryData<string, DiagnosticDescriptor> data = [];
        foreach (DiagnosticDescriptor descriptor in DiscoverDescriptors())
        {
            data.Add(descriptor.Id, descriptor);
        }

        return data;
    }

    public static TheoryData<string, DiagnosticDescriptor, ExpectedDescriptorMetadata> ExpectedMetadataData()
    {
        IReadOnlyDictionary<string, ExpectedDescriptorMetadata> expectedMetadata = GetExpectedMetadata();
        TheoryData<string, DiagnosticDescriptor, ExpectedDescriptorMetadata> data = [];

        foreach (DiagnosticDescriptor descriptor in DiscoverDescriptors())
        {
            Assert.True(
                expectedMetadata.TryGetValue(descriptor.Id, out ExpectedDescriptorMetadata? expected),
                $"Missing expected descriptor metadata for {descriptor.Id}.");

            data.Add(descriptor.Id, descriptor, expected);
        }

        return data;
    }

    public static ImmutableArray<DiagnosticDescriptor> DiscoverDescriptors()
    {
        ImmutableArray<DiagnosticDescriptor>.Builder descriptors = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();

        foreach (Type analyzerType in DiscoverAnalyzerTypes())
        {
            DiagnosticAnalyzer analyzer = CreateAnalyzer(analyzerType);
            descriptors.AddRange(analyzer.SupportedDiagnostics);
        }

        descriptors.Sort(static (left, right) => string.Compare(left.Id, right.Id, StringComparison.Ordinal));
        return descriptors.ToImmutable();
    }

    [Theory]
    [MemberData(nameof(DescriptorData))]
    public void DescriptorMetadataShouldBeCompleteAndDocumented(string ruleId, DiagnosticDescriptor descriptor)
    {
        Assert.True(IsMoqRuleId(ruleId), $"{ruleId} must match MoqNNNN.");
        Assert.False(string.IsNullOrWhiteSpace(descriptor.Title.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.Description.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.True(IsAllowedCategory(descriptor.Category), $"{descriptor.Id} uses unexpected category '{descriptor.Category}'.");
        AssertRuleDocMatchesDescriptor(descriptor);
        AssertHelpLinkMatchesRuleId(descriptor);
    }

    [Theory]
    [MemberData(nameof(ExpectedMetadataData))]
    public void DescriptorMetadataShouldMatchExpectedText(
        string ruleId,
        DiagnosticDescriptor descriptor,
        ExpectedDescriptorMetadata expected)
    {
        Assert.Equal(expected.Title, descriptor.Title.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(expected.MessageFormat, descriptor.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(expected.Description, descriptor.Description.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(expected.Category, descriptor.Category);
        Assert.Equal(expected.DefaultSeverity, descriptor.DefaultSeverity);
        Assert.Equal(expected.IsEnabledByDefault, descriptor.IsEnabledByDefault);
        Assert.Equal(ruleId, expected.RuleId);
    }

    [Fact]
    public void RuleDocsShouldMatchDiscoveredDescriptors()
    {
        SortedSet<string> descriptorIds = new(DiscoverDescriptors().Select(descriptor => descriptor.Id), StringComparer.Ordinal);
        SortedSet<string> documentedIds = new(
            GetRuleDocsDirectory()
                .EnumerateFiles("Moq*.md")
                .Select(file => Path.GetFileNameWithoutExtension(file.Name)),
            StringComparer.Ordinal);

        Assert.Equal(descriptorIds, documentedIds);
    }

    [Fact]
    public void ExpectedMetadataShouldCoverDiscoveredDescriptors()
    {
        SortedSet<string> descriptorIds = new(DiscoverDescriptors().Select(descriptor => descriptor.Id), StringComparer.Ordinal);
        SortedSet<string> expectedIds = new(GetExpectedMetadata().Keys, StringComparer.Ordinal);

        Assert.Equal(descriptorIds, expectedIds);
    }

    private static DiagnosticAnalyzer CreateAnalyzer(Type analyzerType)
    {
        DiagnosticAnalyzer? analyzer = (DiagnosticAnalyzer?)Activator.CreateInstance(analyzerType);
        Assert.NotNull(analyzer);
        return analyzer;
    }

    private static ImmutableArray<Type> DiscoverAnalyzerTypes()
    {
        Assembly analyzersAssembly = typeof(NoSealedClassMocksAnalyzer).Assembly;
        return analyzersAssembly
            .GetTypes()
            .Where(type =>
                string.Equals(type.Namespace, "Moq.Analyzers", StringComparison.Ordinal) &&
                !type.IsAbstract &&
                typeof(DiagnosticAnalyzer).IsAssignableFrom(type) &&
                type.GetCustomAttribute<DiagnosticAnalyzerAttribute>() != null)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static void AssertRuleDocMatchesDescriptor(DiagnosticDescriptor descriptor)
    {
        FileInfo ruleDoc = new(Path.Combine(GetRuleDocsDirectory().FullName, $"{descriptor.Id}.md"));
        Assert.True(ruleDoc.Exists, $"Missing rule doc for {descriptor.Id}.");

        string enabledValue = ReadRuleDocTableValue(ruleDoc, "Enabled");
        string severityValue = ReadRuleDocTableValue(ruleDoc, "Severity");
        Assert.Equal(descriptor.IsEnabledByDefault.ToString(), enabledValue);
        Assert.Equal(descriptor.DefaultSeverity.ToString(), severityValue);
    }

    private static string ReadRuleDocTableValue(FileInfo ruleDoc, string key)
    {
        using StreamReader reader = ruleDoc.OpenText();
        while (reader.ReadLine() is { } line)
        {
            string[] cells = line.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (cells.Length == 2 && string.Equals(cells[0], key, StringComparison.Ordinal))
            {
                return cells[1];
            }
        }

        throw new InvalidOperationException($"Could not find '{key}' in {ruleDoc.FullName}.");
    }

    private static void AssertHelpLinkMatchesRuleId(DiagnosticDescriptor descriptor)
    {
        Assert.False(string.IsNullOrWhiteSpace(descriptor.HelpLinkUri));
        Assert.True(Uri.TryCreate(descriptor.HelpLinkUri, UriKind.Absolute, out Uri? helpUri));
        Assert.NotNull(helpUri);
        Assert.Equal("github.com", helpUri.Host);
        Assert.EndsWith($"/docs/rules/{descriptor.Id}.md", helpUri.AbsolutePath, StringComparison.Ordinal);
    }

    private static DirectoryInfo GetRuleDocsDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            DirectoryInfo candidate = new(Path.Combine(directory.FullName, "docs", "rules"));
            if (candidate.Exists)
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find docs/rules from the test output directory.");
    }

    private static bool IsMoqRuleId(string ruleId)
    {
        return ruleId.Length == 7 &&
            ruleId.StartsWith("Moq", StringComparison.Ordinal) &&
            ruleId.Skip(3).All(char.IsDigit);
    }

    private static bool IsAllowedCategory(string category)
    {
        return string.Equals(category, DiagnosticCategory.Usage, StringComparison.Ordinal) ||
            string.Equals(category, DiagnosticCategory.Correctness, StringComparison.Ordinal) ||
            string.Equals(category, DiagnosticCategory.BestPractice, StringComparison.Ordinal);
    }

    private static IReadOnlyDictionary<string, ExpectedDescriptorMetadata> GetExpectedMetadata()
    {
        return new Dictionary<string, ExpectedDescriptorMetadata>(StringComparer.Ordinal)
        {
            [DiagnosticIds.SealedClassCannotBeMocked] = new(
                DiagnosticIds.SealedClassCannotBeMocked,
                "Moq: Sealed class mocked",
                "Sealed class '{0}' cannot be mocked",
                "Sealed classes cannot be mocked.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.NoConstructorArgumentsForInterfaceMockRuleId] = new(
                DiagnosticIds.NoConstructorArgumentsForInterfaceMockRuleId,
                "Mock<T> construction must not specify parameters for interfaces",
                "Mocked interface '{0}' cannot have constructor parameters {1}",
                "Mocked interface cannot have constructor parameters.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.NoMatchingConstructorRuleId] = new(
                DiagnosticIds.NoMatchingConstructorRuleId,
                "Mock<T> construction must call an existing type constructor",
                "Could not find a matching constructor for type '{0}' with arguments {1}",
                "Could not find a matching constructor for arguments.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.InternalTypeMustHaveInternalsVisibleTo] = new(
                DiagnosticIds.InternalTypeMustHaveInternalsVisibleTo,
                "Moq: Internal type requires InternalsVisibleTo",
                "Internal type '{0}' requires [InternalsVisibleTo(\"DynamicProxyGenAssembly2\")] in its assembly to be mocked",
                "Mocking internal types requires the assembly to grant access to Castle DynamicProxy via InternalsVisibleTo.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.LoggerShouldNotBeMocked] = new(
                DiagnosticIds.LoggerShouldNotBeMocked,
                "Moq: ILogger mocked",
                "ILogger should not be mocked; use {0} or FakeLogger from Microsoft.Extensions.Diagnostics.Testing instead",
                "Mocking ILogger is unnecessary and fragile. Use NullLogger.Instance (for ILogger) or NullLogger<T>.Instance (for ILogger<T>) for tests that ignore logging, or FakeLogger from Microsoft.Extensions.Diagnostics.Testing for tests that verify log output.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.BadCallbackParameters] = new(
                DiagnosticIds.BadCallbackParameters,
                "Moq: Bad callback parameters",
                "Callback signature for '{0}' must match the signature of the mocked method",
                "Callback signature must match the signature of the mocked method.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.PropertySetupUsedForMethod] = new(
                DiagnosticIds.PropertySetupUsedForMethod,
                "Moq: Property setup used for a method",
                "SetupGet/SetupSet/SetupProperty should be used for properties, not for methods like '{0}'",
                "SetupGet/SetupSet/SetupProperty should be used for properties, not for methods.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.SetupOnlyUsedForOverridableMembers] = new(
                DiagnosticIds.SetupOnlyUsedForOverridableMembers,
                "Moq: Invalid setup parameter",
                "Setup should be used only for overridable members, but '{0}' is not overridable",
                "Setup should be used only for overridable members.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Error,
                true),
            [DiagnosticIds.AsyncUsesReturnsAsyncInsteadOfResult] = new(
                DiagnosticIds.AsyncUsesReturnsAsyncInsteadOfResult,
                "Moq: Invalid setup parameter",
                "Setup of async method '{0}' should use ReturnsAsync instead of .Result",
                "Setup of async methods should use ReturnsAsync instead of .Result.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Error,
                true),
            [DiagnosticIds.RaiseEventArgumentsShouldMatchEventSignature] = new(
                DiagnosticIds.RaiseEventArgumentsShouldMatchEventSignature,
                "Moq: Raise event arguments should match event signature",
                "Raise event arguments should match the '{0}' event delegate signature",
                "Raise event arguments should match the event delegate signature.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.MethodSetupShouldSpecifyReturnValue] = new(
                DiagnosticIds.MethodSetupShouldSpecifyReturnValue,
                "Method setup should specify a return value",
                "Method setup for '{0}' should specify a return value",
                "Method setups that return a value should use Returns(), ReturnsAsync(), Throws(), or ThrowsAsync() to specify a return value.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature] = new(
                DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature,
                "Moq: Raises event arguments should match event signature",
                "Raises event arguments should match the '{0}' event delegate signature",
                "Raises event arguments should match the event delegate signature.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.EventSetupHandlerShouldMatchEventType] = new(
                DiagnosticIds.EventSetupHandlerShouldMatchEventType,
                "Moq: Event setup handler type should match event delegate type",
                "Event setup handler type should match the event delegate type for '{0}'",
                "Event setup handler type should match the event delegate type.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.ReturnsAsyncShouldBeUsedForAsyncMethods] = new(
                DiagnosticIds.ReturnsAsyncShouldBeUsedForAsyncMethods,
                "Moq: Invalid Returns usage with async method",
                "Async method '{0}' setups should use ReturnsAsync instead of Returns with async lambda",
                "Async method setups should use ReturnsAsync instead of Returns with async lambda.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.SetupSequenceOnlyUsedForOverridableMembers] = new(
                DiagnosticIds.SetupSequenceOnlyUsedForOverridableMembers,
                "Moq: Invalid SetupSequence parameter",
                "SetupSequence should be used only for overridable members, but '{0}' is not overridable",
                "SetupSequence should be used only for overridable members.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Error,
                true),
            [DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod] = new(
                DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod,
                "Moq: Returns() delegate type mismatch on async method",
                "Returns() delegate for async method '{0}' should return '{2}', not '{1}'. Use ReturnsAsync() or wrap with Task.FromResult().",
                "Returns() delegate on async method setup should return Task/ValueTask. Use ReturnsAsync() or wrap with Task.FromResult().",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.VerifyOnlyUsedForOverridableMembers] = new(
                DiagnosticIds.VerifyOnlyUsedForOverridableMembers,
                "Moq: Invalid verify parameter",
                "Verify should be used only for overridable members, but '{0}' is not overridable",
                "Verify should be used only for overridable members.",
                DiagnosticCategory.Correctness,
                DiagnosticSeverity.Error,
                true),
            [DiagnosticIds.AsShouldOnlyBeUsedForInterfacesRuleId] = new(
                DiagnosticIds.AsShouldOnlyBeUsedForInterfacesRuleId,
                "Moq: Invalid As type parameter",
                "Mock.As() should take interfaces only, but '{0}' is not an interface",
                "Mock.As() should take interfaces only.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Error,
                true),
            [DiagnosticIds.MockGetShouldNotTakeLiterals] = new(
                DiagnosticIds.MockGetShouldNotTakeLiterals,
                "Moq: Mock.Get() should not take literals",
                "Mock.Get() should not take literal '{0}'",
                "Mock.Get() should not take literals.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.LinqToMocksExpressionShouldBeValid] = new(
                DiagnosticIds.LinqToMocksExpressionShouldBeValid,
                "Moq: Invalid LINQ to Mocks expression",
                "Invalid member '{0}' in LINQ to Mocks expression",
                "LINQ to Mocks expression contains non-virtual member that cannot be mocked.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.SetExplicitMockBehavior] = new(
                DiagnosticIds.SetExplicitMockBehavior,
                "Moq: Explicitly choose a mock behavior",
                "Explicitly choose a mocking behavior for {0} instead of relying on the default (Loose) behavior",
                "Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior.",
                DiagnosticCategory.BestPractice,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.SetStrictMockBehavior] = new(
                DiagnosticIds.SetStrictMockBehavior,
                "Moq: Set MockBehavior to Strict",
                "Explicitly set the Strict mocking behavior for '{0}'",
                "Explicitly set the Strict mocking behavior.",
                DiagnosticCategory.BestPractice,
                DiagnosticSeverity.Info,
                true),
            [DiagnosticIds.RedundantTimesSpecification] = new(
                DiagnosticIds.RedundantTimesSpecification,
                "Moq: Redundant Times specification",
                "Redundant {0} specification can be removed as it is the default for Verify calls",
                "Redundant Times.AtLeastOnce() specification can be removed as it is the default for Verify calls.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Info,
                true),
            [DiagnosticIds.MockRepositoryVerifyNotCalled] = new(
                DiagnosticIds.MockRepositoryVerifyNotCalled,
                "Moq: MockRepository.Verify() should be called",
                "MockRepository '{0}' should have Verify() called",
                "MockRepository.Verify() should be called to verify all mocks created through the repository.",
                DiagnosticCategory.BestPractice,
                DiagnosticSeverity.Warning,
                true),
            [DiagnosticIds.ProtectedSetupUsesItMatcherInsteadOfItExpr] = new(
                DiagnosticIds.ProtectedSetupUsesItMatcherInsteadOfItExpr,
                "Moq: Protected setup should use ItExpr",
                "Protected member setup uses 'It.{0}' which is not compatible with string-based overloads; use an ItExpr matcher instead",
                "Protected member setups using string-based overloads must use ItExpr matchers instead of It matchers.",
                DiagnosticCategory.Usage,
                DiagnosticSeverity.Warning,
                true),
        };
    }

    public sealed record ExpectedDescriptorMetadata(
        string RuleId,
        string Title,
        string MessageFormat,
        string Description,
        string Category,
        DiagnosticSeverity DefaultSeverity,
        bool IsEnabledByDefault);
}
