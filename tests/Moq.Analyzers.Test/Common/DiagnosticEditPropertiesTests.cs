using System.Collections.Immutable;
using Moq.Analyzers.Common;
using Xunit;

namespace Moq.Analyzers.Test.Common;

public class DiagnosticEditPropertiesTests
{
    [Fact]
    public void ToImmutableDictionary_RoundTripsProperties()
    {
        var props = new DiagnosticEditProperties { TypeOfEdit = DiagnosticEditProperties.EditType.Insert, EditPosition = 2 };
        var dict = props.ToImmutableDictionary();
        Assert.Equal("Insert", dict[DiagnosticEditProperties.EditTypeKey]);
        Assert.Equal("2", dict[DiagnosticEditProperties.EditPositionKey]);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Succeeds_WithValidDictionary()
    {
        var dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "Replace")
            .Add(DiagnosticEditProperties.EditPositionKey, "5");
        var result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out var props);
        Assert.True(result);
        Assert.NotNull(props);
        Assert.Equal(DiagnosticEditProperties.EditType.Replace, props!.TypeOfEdit);
        Assert.Equal(5, props.EditPosition);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Fails_WhenEditTypeKeyMissing()
    {
        var dict = ImmutableDictionary<string, string?>.Empty.Add(DiagnosticEditProperties.EditPositionKey, "1");
        var result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out var props);
        Assert.False(result);
        Assert.Null(props);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Fails_WhenEditPositionKeyMissing()
    {
        var dict = ImmutableDictionary<string, string?>.Empty.Add(DiagnosticEditProperties.EditTypeKey, "Insert");
        var result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out var props);
        Assert.False(result);
        Assert.Null(props);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Fails_WhenEditTypeInvalid()
    {
        var dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "NotAType")
            .Add(DiagnosticEditProperties.EditPositionKey, "1");
        var result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out var props);
        Assert.False(result);
        Assert.Null(props);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_Fails_WhenEditPositionInvalid()
    {
        var dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "Insert")
            .Add(DiagnosticEditProperties.EditPositionKey, "notAnInt");
        var result = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out var props);
        Assert.False(result);
        Assert.Null(props);
    }
} 