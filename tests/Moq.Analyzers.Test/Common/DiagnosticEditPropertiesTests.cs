using System.Collections.Immutable;
using Moq.Analyzers.Common;
using Xunit;

namespace Moq.Analyzers.Test.Common;

public class DiagnosticEditPropertiesTests
{
    [Fact]
    public void ToImmutableDictionary_RoundTripsProperties()
    {
        DiagnosticEditProperties props = new DiagnosticEditProperties { TypeOfEdit = DiagnosticEditProperties.EditType.Insert, EditPosition = 2 };
        ImmutableDictionary<string, string?> dict = props.ToImmutableDictionary();
        Assert.Equal("Insert", dict[DiagnosticEditProperties.EditTypeKey]);
        Assert.Equal("2", dict[DiagnosticEditProperties.EditPositionKey]);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Succeeds_WithValidDictionary()
    {
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "Replace")
            .Add(DiagnosticEditProperties.EditPositionKey, "5");
        bool result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? props);
        Assert.True(result);
        Assert.NotNull(props);
        Assert.Equal(DiagnosticEditProperties.EditType.Replace, props!.TypeOfEdit);
        Assert.Equal(5, props.EditPosition);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Fails_WhenEditTypeKeyMissing()
    {
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty.Add(DiagnosticEditProperties.EditPositionKey, "1");
        bool result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? props);
        Assert.False(result);
        Assert.Null(props);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Fails_WhenEditPositionKeyMissing()
    {
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty.Add(DiagnosticEditProperties.EditTypeKey, "Insert");
        bool result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? props);
        Assert.False(result);
        Assert.Null(props);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Fails_WhenEditTypeInvalid()
    {
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "NotAType")
            .Add(DiagnosticEditProperties.EditPositionKey, "1");
        bool result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? props);
        Assert.False(result);
        Assert.Null(props);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Fails_WhenEditPositionInvalid()
    {
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "Insert")
            .Add(DiagnosticEditProperties.EditPositionKey, "notAnInt");
        bool result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? props);
        Assert.False(result);
        Assert.Null(props);
    }
}
