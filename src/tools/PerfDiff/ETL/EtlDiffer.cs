// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Stacks;

namespace PerfDiff.ETL;

internal static class EtlDiffer
{
    public static bool TryCompareETL(string sourceEtlPath, string baselineEtlPath, out bool regression)
    {
        regression = false;
        CallTree sourceCallTree = GetCallTree(sourceEtlPath);
        CallTree baselineCallTree = GetCallTree(baselineEtlPath);
        ImmutableArray<OverWeightResult> report = GenerateOverweightReport(sourceCallTree, baselineCallTree);

        // print results
        Console.WriteLine(string.Join(Environment.NewLine, report.Take(10)));
        return true;
    }

    private static CallTree GetCallTree(string eltPath)
    {
        TraceProcess traceProcess = GetTraceProcessFromETLFile(eltPath);
        StackSource stackSource = CreateStackSourceFromTraceProcess(traceProcess);
        return CreateCallTreeFromStackSource(stackSource);
    }

    public static TraceProcess GetTraceProcessFromETLFile(string eltPath)
    {
        TraceLog? traceLog = TraceLog.OpenOrConvert(eltPath);
        return traceLog.Processes
            .First(p => p.Name.Equals("dotnet", StringComparison.OrdinalIgnoreCase));
    }

    public static StackSource CreateStackSourceFromTraceProcess(TraceProcess process)
    {
        TraceEvents? events = process.EventsInProcess;
        double start = Math.Max(events.StartTimeRelativeMSec, process.StartTimeRelativeMsec);
        double end = Math.Min(events.EndTimeRelativeMSec, process.EndTimeRelativeMsec);
        events = events.FilterByTime(start, end);
        events = events.Filter(x => x is SampledProfileTraceData && x.ProcessID == process.ProcessID);

        using SymbolReader symbolReader = new SymbolReader(new StringWriter(), @"SRV*https://msdl.microsoft.com/download/symbols");
        symbolReader.SecurityCheck = path => true;

        TraceLog? traceLog = process.Log;
        foreach (TraceLoadedModule? module in process.LoadedModules)
        {
            traceLog.CodeAddresses.LookupSymbolsForModule(symbolReader, module.ModuleFile);
        }

        return new TraceEventStackSource(events);
    }

    public static CallTree CreateCallTreeFromStackSource(StackSource stackSource)
    {
        CallTree calltree = new CallTree(ScalingPolicyKind.ScaleToData);
        calltree.StackSource = stackSource;
        return calltree;
    }

    public static ImmutableArray<OverWeightResult> GenerateOverweightReport(CallTree source, CallTree baseline)
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

        static ImmutableArray<OverWeightResult> ComputeOverweights(float sourceTotal, Dictionary<string, float> sourceData, float baselineTotal, Dictionary<string, float> baselineData)
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

            results.Sort((left, right) =>
            {
                if (left.Interest < right.Interest)
                    return 1;

                if (left.Interest > right.Interest)
                    return -1;

                if (left.Overweight < right.Overweight)
                    return 1;

                if (left.Overweight > right.Overweight)
                    return -1;

                if (left.Delta < right.Delta)
                    return -1;

                if (left.Delta > right.Delta)
                    return 1;

                return 0;
            });

            return results.ToImmutable();
        }
    }
}
