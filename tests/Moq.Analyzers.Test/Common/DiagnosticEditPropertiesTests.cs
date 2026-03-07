namespace Moq.Analyzers.Test.Common;

public class DiagnosticEditPropertiesTests
{
    [Fact]
    public void ToImmutableDictionary_Insert_RoundTrips()
    {
        // Arrange
        DiagnosticEditProperties original = new()
        {
            TypeOfEdit = DiagnosticEditProperties.EditType.Insert,
            EditPosition = 3,
        };

        // Act
        ImmutableDictionary<string, string?> dict = original.ToImmutableDictionary();
        bool success = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(DiagnosticEditProperties.EditType.Insert, result.TypeOfEdit);
        Assert.Equal(3, result.EditPosition);
    }

    [Fact]
    public void ToImmutableDictionary_Replace_RoundTrips()
    {
        // Arrange
        DiagnosticEditProperties original = new()
        {
            TypeOfEdit = DiagnosticEditProperties.EditType.Replace,
            EditPosition = 0,
        };

        // Act
        ImmutableDictionary<string, string?> dict = original.ToImmutableDictionary();
        bool success = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(DiagnosticEditProperties.EditType.Replace, result.TypeOfEdit);
        Assert.Equal(0, result.EditPosition);
    }

    [Fact]
    public void ToImmutableDictionary_ContainsExpectedKeys()
    {
        // Arrange
        DiagnosticEditProperties props = new()
        {
            TypeOfEdit = DiagnosticEditProperties.EditType.Insert,
            EditPosition = 5,
        };

        // Act
        ImmutableDictionary<string, string?> dict = props.ToImmutableDictionary();

        // Assert
        Assert.True(dict.ContainsKey(DiagnosticEditProperties.EditTypeKey));
        Assert.True(dict.ContainsKey(DiagnosticEditProperties.EditPositionKey));
        Assert.Equal("Insert", dict[DiagnosticEditProperties.EditTypeKey]);
        Assert.Equal("5", dict[DiagnosticEditProperties.EditPositionKey]);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_MissingEditTypeKey_ReturnsFalse()
    {
        // Arrange
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditPositionKey, "0");

        // Act
        bool success = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_MissingEditPositionKey_ReturnsFalse()
    {
        // Arrange
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "Insert");

        // Act
        bool success = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_InvalidEditType_ReturnsFalse()
    {
        // Arrange
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "InvalidValue")
            .Add(DiagnosticEditProperties.EditPositionKey, "0");

        // Act
        bool success = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_NonNumericEditPosition_ReturnsFalse()
    {
        // Arrange
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "Insert")
            .Add(DiagnosticEditProperties.EditPositionKey, "notAnumber");

        // Act
        bool success = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_NegativeEditPosition_ReturnsFalse()
    {
        // Arrange
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, "Replace")
            .Add(DiagnosticEditProperties.EditPositionKey, "-1");

        // Act
        bool success = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetFromImmutableDictionary_EmptyDictionary_ReturnsFalse()
    {
        // Arrange
        ImmutableDictionary<string, string?> dict = ImmutableDictionary<string, string?>.Empty;

        // Act
        bool success = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void TryGetFromImmutableDictionary_ValidPositions_Succeeds(int position)
    {
        // Arrange
        DiagnosticEditProperties original = new()
        {
            TypeOfEdit = DiagnosticEditProperties.EditType.Insert,
            EditPosition = position,
        };

        // Act
        ImmutableDictionary<string, string?> dict = original.ToImmutableDictionary();
        bool success = DiagnosticEditProperties.TryGetFromImmutableDictionary(dict, out DiagnosticEditProperties? result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(position, result.EditPosition);
    }
}
