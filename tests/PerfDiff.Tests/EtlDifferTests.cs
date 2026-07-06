using System.Collections.Immutable;
using PerfDiff.ETL;
using Xunit;

namespace PerfDiff.Tests;

public sealed class EtlDifferTests
{
    [Fact]
    public void HasRegression_WithPositiveInterestingOverweightResult_ReturnsTrue()
    {
        ImmutableArray<OverWeightResult> report =
        [
            new("HotPath", Before: 100, After: 180, Delta: 80, Overweight: 150, Percent: 10, Interest: 2),
        ];

        Assert.True(EtlDiffer.HasRegression(report));
    }

    [Fact]
    public void HasRegression_WithEmptyReport_ReturnsFalse()
    {
        Assert.False(EtlDiffer.HasRegression(ImmutableArray<OverWeightResult>.Empty));
    }

    [Fact]
    public void HasRegression_WithUnderThresholdReport_ReturnsFalse()
    {
        ImmutableArray<OverWeightResult> report =
        [
            new("Noise", Before: 100, After: 101, Delta: 1, Overweight: 100, Percent: 1, Interest: 0),
        ];

        Assert.False(EtlDiffer.HasRegression(report));
    }

    [Fact]
    public void HasRegression_WithInterestingImprovement_ReturnsFalse()
    {
        ImmutableArray<OverWeightResult> report =
        [
            new("FasterPath", Before: 180, After: 100, Delta: -80, Overweight: 150, Percent: -10, Interest: 2),
        ];

        Assert.False(EtlDiffer.HasRegression(report));
    }

    [Fact]
    public void TryCompareETL_WhenTraceFileCannotBeOpened_ReturnsFalseAndClearsRegression()
    {
        bool compareSucceeded = EtlDiffer.TryCompareETL("missing-source.etl.zip", "missing-baseline.etl.zip", out bool regression);

        Assert.False(compareSucceeded);
        Assert.False(regression);
    }

    [Fact]
    public void ComputeOverweights_WithInterestingGrowth_ReturnsSortedReport()
    {
        Dictionary<string, float> sourceData = new(StringComparer.Ordinal)
        {
            ["Hot"] = 50,
            ["Warm"] = 60,
            ["Neutral"] = 95,
        };
        Dictionary<string, float> baselineData = new(StringComparer.Ordinal)
        {
            ["Hot"] = 10,
            ["Warm"] = 40,
            ["Neutral"] = 45,
            ["Missing"] = 5,
        };

        ImmutableArray<OverWeightResult> report = EtlDiffer.ComputeOverweights(200, sourceData, 100, baselineData);

        Assert.Equal(["Hot", "Neutral", "Warm"], report.Select(static result => result.Name));
        foreach (OverWeightResult result in report)
        {
            Assert.True(result.Interest > 0);
        }
    }

    [Fact]
    public void ComputeOverweights_WithEqualInterestAndOverweight_SortsBySmallestDelta()
    {
        Dictionary<string, float> sourceData = new(StringComparer.Ordinal)
        {
            ["SmallDelta"] = 20,
            ["LargeDelta"] = 40,
        };
        Dictionary<string, float> baselineData = new(StringComparer.Ordinal)
        {
            ["LargeDelta"] = 20,
            ["SmallDelta"] = 10,
        };

        ImmutableArray<OverWeightResult> report = EtlDiffer.ComputeOverweights(200, sourceData, 100, baselineData);

        Assert.Equal(["SmallDelta", "LargeDelta"], report.Select(static result => result.Name));
    }

    [Fact]
    public void ComputeOverweights_WithNoSharedSymbols_ReturnsEmpty()
    {
        Dictionary<string, float> sourceData = new(StringComparer.Ordinal)
        {
            ["SourceOnly"] = 50,
        };
        Dictionary<string, float> baselineData = new(StringComparer.Ordinal)
        {
            ["BaselineOnly"] = 10,
        };

        ImmutableArray<OverWeightResult> report = EtlDiffer.ComputeOverweights(200, sourceData, 100, baselineData);

        Assert.True(report.IsEmpty);
    }
}
