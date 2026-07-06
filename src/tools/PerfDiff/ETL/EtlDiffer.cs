// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Stacks;

namespace PerfDiff.ETL;

internal static class EtlDiffer
{
    [ExcludeFromCodeCoverage(Justification = "TraceEvent requires real ETL files; pure regression verdict logic is covered separately.")]
    public static bool TryCompareETL(string sourceEtlPath, string baselineEtlPath, out bool regression)
    {
        regression = false;

        try
        {
            using TraceLog sourceTraceLog = TraceLog.OpenOrConvert(sourceEtlPath)
                ?? throw new InvalidOperationException($"Failed to open ETL trace file: {sourceEtlPath}");
            using TraceLog baselineTraceLog = TraceLog.OpenOrConvert(baselineEtlPath)
                ?? throw new InvalidOperationException($"Failed to open ETL trace file: {baselineEtlPath}");

            CallTree sourceCallTree = GetCallTree(sourceTraceLog, sourceEtlPath);
            CallTree baselineCallTree = GetCallTree(baselineTraceLog, baselineEtlPath);
            ImmutableArray<OverWeightResult> report = GenerateOverweightReport(sourceCallTree, baselineCallTree);

            Console.WriteLine(string.Join(Environment.NewLine, report.Take(10)));
            regression = HasRegression(report);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"ETL comparison failed: {ex.Message}");
            return false;
        }
    }

    internal static bool HasRegression(ImmutableArray<OverWeightResult> report)
    {
        if (report.IsDefaultOrEmpty)
        {
            return false;
        }

        foreach (OverWeightResult result in report)
        {
            Debug.Assert(result.Interest >= 0, "Interest is built from non-negative threshold hits.");
            if (result.Delta > 0 && result.Interest > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static CallTree GetCallTree(TraceLog traceLog, string eltPath)
    {
        TraceProcess traceProcess = GetTraceProcessFromTraceLog(traceLog, eltPath);
        StackSource stackSource = CreateStackSourceFromTraceProcess(traceProcess);
        return CreateCallTreeFromStackSource(stackSource);
    }

    private static TraceProcess GetTraceProcessFromTraceLog(TraceLog traceLog, string eltPath)
    {
        TraceProcess? process = traceLog.Processes
            .FirstOrDefault(static p => string.Equals(p.Name, "dotnet", StringComparison.OrdinalIgnoreCase) && p.EventsInProcess is not null);

        if (process is null)
        {
            string available = string.Join(", ", traceLog.Processes.Select(static p => p.Name));
            throw new InvalidOperationException(
                $"No 'dotnet' process found in ETL file: {eltPath}. Available processes: {available}");
        }

        return process;
    }

    private static StackSource CreateStackSourceFromTraceProcess(TraceProcess process)
    {
        // Defensive null guard: EventsInProcess was checked during process selection in
        // GetTraceProcessFromTraceLog, but TraceProcess is mutable, so guard against race conditions.
        TraceEvents events = process.EventsInProcess
            ?? throw new ArgumentException("Process has no events.", nameof(process));
        double start = Math.Max(events.StartTimeRelativeMSec, process.StartTimeRelativeMsec);
        double end = Math.Min(events.EndTimeRelativeMSec, process.EndTimeRelativeMsec);
        events = events.FilterByTime(start, end);
        events = events.Filter(x => x is SampledProfileTraceData && x.ProcessID == process.ProcessID);

        using SymbolReader symbolReader = new SymbolReader(new StringWriter(), @"SRV*https://msdl.microsoft.com/download/symbols");
        symbolReader.SecurityCheck = static path => true;

        TraceLog traceLog = process.Log;
        foreach (TraceLoadedModule? module in process.LoadedModules)
        {
            traceLog.CodeAddresses.LookupSymbolsForModule(symbolReader, module.ModuleFile);
        }

        return new TraceEventStackSource(events);
    }

    private static CallTree CreateCallTreeFromStackSource(StackSource stackSource)
    {
        CallTree calltree = new CallTree(ScalingPolicyKind.ScaleToData);
        calltree.StackSource = stackSource;
        return calltree;
    }

    private static ImmutableArray<OverWeightResult> GenerateOverweightReport(CallTree source, CallTree baseline)
    {
        float sourceTotal = LoadTrace(source, out Dictionary<string, float> sourceData);
        float baselineTotal = LoadTrace(baseline, out Dictionary<string, float> baselineData);

        if (sourceTotal != baselineTotal)
        {
            return ComputeOverweights(sourceTotal, sourceData, baselineTotal, baselineData);
        }

        return ImmutableArray<OverWeightResult>.Empty;

        static float LoadTrace(CallTree callTree, out Dictionary<string, float> data)
        {
            data = new Dictionary<string, float>();
            float total = 0;
            foreach (CallTreeNodeBase? node in callTree.ByID)
            {
                if (node.InclusiveMetric == 0)
                {
                    continue;
                }

                string key = node.Name;
                data.TryGetValue(key, out float weight);
                data[key] = weight + node.InclusiveMetric;

                total += node.ExclusiveMetric;
            }

            return total;
        }
    }

    internal static ImmutableArray<OverWeightResult> ComputeOverweights(float sourceTotal, Dictionary<string, float> sourceData, float baselineTotal, Dictionary<string, float> baselineData)
    {
        float totalDelta = sourceTotal - baselineTotal;
        float growth = sourceTotal / baselineTotal;
        ImmutableArray<OverWeightResult>.Builder results = ImmutableArray.CreateBuilder<OverWeightResult>();
        foreach (string key in baselineData.Keys)
        {
            // skip symbols that are not in both traces
            if (!sourceData.ContainsKey(key))
            {
                continue;
            }

            float baselineValue = baselineData[key];
            float sourceValue = sourceData[key];
            float expectedDelta = baselineValue * (growth - 1);
            float delta = sourceValue - baselineValue;
            float overweight = delta / expectedDelta * 100;
            float percent = delta / totalDelta;
            // Calculate interest level
            int interest = Math.Abs(overweight) > 110 ? 1 : 0;
            interest += Math.Abs(percent) > 5 ? 1 : 0;
            interest += Math.Abs(percent) > 20 ? 1 : 0;
            interest += Math.Abs(percent) > 100 ? 1 : 0;
            interest += sourceValue / sourceTotal < 0.95 ? 1 : 0;  // Ignore top of the stack frames
            interest += sourceValue / sourceTotal < 0.75 ? 1 : 0;  // Bonus point for being further down the stack.

            results.Add(new OverWeightResult
            (
                Name: key,
                Before: baselineValue,
                After: sourceValue,
                Delta: delta,
                Overweight: overweight,
                Percent: percent,
                Interest: interest
            ));
        }

        return results
            .OrderByDescending(static result => result.Interest)
            .ThenByDescending(static result => result.Overweight)
            .ThenBy(static result => result.Delta)
            .ToImmutableArray();
    }
}
