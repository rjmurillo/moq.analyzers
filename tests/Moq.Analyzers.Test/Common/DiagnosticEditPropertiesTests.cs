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
    public void ToImmutableDictionary_AllEnumValues_RoundTrips()
    {
        foreach (DiagnosticEditProperties.EditType type in Enum.GetValues(typeof(DiagnosticEditProperties.EditType)))
        {
            DiagnosticEditProperties props = new DiagnosticEditProperties { TypeOfEdit = type, EditPosition = 0 };
            ImmutableDictionary<string, string?> dict = props.ToImmutableDictionary();
            Assert.Equal(type.ToString(), dict[DiagnosticEditProperties.EditTypeKey]);
            Assert.Equal("0", dict[DiagnosticEditProperties.EditPositionKey]);
            Assert.True(DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? roundTripped));
            Assert.Equal(type, roundTripped!.TypeOfEdit);
        }
    }

    [Fact]
    public void ToImmutableDictionary_HandlesNegativeAndLargeEditPosition()
    {
        DiagnosticEditProperties propsNeg = new DiagnosticEditProperties { TypeOfEdit = DiagnosticEditProperties.EditType.Insert, EditPosition = -1 };
        ImmutableDictionary<string, string?> dictNeg = propsNeg.ToImmutableDictionary();
        Assert.Equal("-1", dictNeg[DiagnosticEditProperties.EditPositionKey]);
        Assert.True(DiagnosticEditProperties.TryGetFromImmutableDictionary(dictNeg, out DiagnosticEditProperties? roundTrippedNeg));
        Assert.Equal(-1, roundTrippedNeg!.EditPosition);

        DiagnosticEditProperties propsLarge = new DiagnosticEditProperties { TypeOfEdit = DiagnosticEditProperties.EditType.Replace, EditPosition = int.MaxValue };
        ImmutableDictionary<string, string?> dictLarge = propsLarge.ToImmutableDictionary();
        Assert.Equal(int.MaxValue.ToString(), dictLarge[DiagnosticEditProperties.EditPositionKey]);
        Assert.True(DiagnosticEditProperties.TryGetFromImmutableDictionary(dictLarge, out DiagnosticEditProperties? roundTrippedLarge));
        Assert.Equal(int.MaxValue, roundTrippedLarge!.EditPosition);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_IgnoresExtraKeys()
    {
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "Insert")
            .Add(DiagnosticEditProperties.EditPositionKey, "1")
            .Add("ExtraKey", "ExtraValue");
        Assert.True(DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? props));
        Assert.Equal(DiagnosticEditProperties.EditType.Insert, props!.TypeOfEdit);
        Assert.Equal(1, props.EditPosition);
    }

    [Fact]
    public void DiagnosticEditProperties_EqualityAndImmutability()
    {
        DiagnosticEditProperties a = new DiagnosticEditProperties { TypeOfEdit = DiagnosticEditProperties.EditType.Insert, EditPosition = 1 };
        DiagnosticEditProperties b = new DiagnosticEditProperties { TypeOfEdit = DiagnosticEditProperties.EditType.Insert, EditPosition = 1 };
        DiagnosticEditProperties c = new DiagnosticEditProperties { TypeOfEdit = DiagnosticEditProperties.EditType.Replace, EditPosition = 1 };
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
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
